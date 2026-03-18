using Cronos;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Scheduler.Data;
using TaskFlow.Scheduler.Services;
using TaskFlow.Shared.Models;

namespace TaskFlow.Scheduler.Workers;

public class SchedulerWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaProducerService _kafka;
    private readonly RedisLockService _lock;
    private readonly ILogger<SchedulerWorker> _logger;

    // Global concurrent execution sayacı
    private static readonly SemaphoreSlim _globalSemaphore = new(10, 10); // max 10 paralel

    public SchedulerWorker(
        IServiceScopeFactory scopeFactory,
        KafkaProducerService kafka,
        RedisLockService lockService,
        ILogger<SchedulerWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _kafka = kafka;
        _lock = lockService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TaskFlow Scheduler başladı");

        // İlk başta missed run'ları recover et
        await RecoverMissedRunsAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndTriggerTasksAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduler döngü hatası");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task CheckAndTriggerTasksAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
        var now = DateTime.UtcNow;

        // Priority'e göre sırala — yüksek öncelikli (düşük sayı) önce çalışır
        var dueTasks = await db.Tasks
            .Where(t => t.IsEnabled
                     && t.ScheduleType != ScheduleType.Manual
                     && t.NextRunAt <= now)
            .OrderBy(t => t.Priority)   // Priority sırası
            .ThenBy(t => t.NextRunAt)
            .ToListAsync();

        _logger.LogInformation("[SCHEDULER] {Count} task tetiklenecek", dueTasks.Count);

        foreach (var task in dueTasks)
        {
            await TriggerTaskAsync(db, task, now);
        }
    }

    private async Task TriggerTaskAsync(SchedulerDbContext db, ScheduledTask task, DateTime scheduledAt)
    {
        // 1. Max concurrent kontrolü
        if (task.ActiveExecutions >= task.MaxConcurrent)
        {
            _logger.LogWarning("[SKIP] {Name} max concurrent ({Max}) doldu", task.Name, task.MaxConcurrent);

            // Missed run kaydet
            if (task.AllowMissedRuns)
            {
                db.MissedRuns.Add(new MissedRun
                {
                    TaskId = task.Id,
                    TaskName = task.Name,
                    ScheduledAt = scheduledAt,
                    Reason = "MaxConcurrent",
                });

                task.NextRunAt = CalculateNextRun(task);
                await db.SaveChangesAsync();
            }
            return;
        }

        // 2. Distributed lock kontrolü
        var lockAcquired = await _lock.AcquireLockAsync(task.Id, task.TimeoutSeconds + 60);
        if (!lockAcquired)
        {
            _logger.LogWarning("[SKIP] {Name} zaten locked", task.Name);

            if (task.AllowMissedRuns)
            {
                db.MissedRuns.Add(new MissedRun
                {
                    TaskId = task.Id,
                    TaskName = task.Name,
                    ScheduledAt = scheduledAt,
                    Reason = "Locked",
                });
                await db.SaveChangesAsync();
            }
            return;
        }

        // 3. Global semaphore — max 10 paralel execution
        var acquired = await _globalSemaphore.WaitAsync(TimeSpan.Zero);
        if (!acquired)
        {
            _logger.LogWarning("[SKIP] {Name} global limit doldu, kuyruğa alındı", task.Name);
            await _lock.ReleaseLockAsync(task.Id);

            task.LastStatus = TaskFlow.Shared.Models.TaskStatus.Queued;
            task.NextRunAt = DateTime.UtcNow.AddSeconds(30); // 30 sn sonra tekrar dene
            await db.SaveChangesAsync();
            return;
        }

        try
        {
            var execution = new TaskExecution
            {
                TaskId = task.Id,
                TaskName = task.Name,
                Status = ExecutionStatus.Running,
                StartedAt = DateTime.UtcNow,
            };
            db.Executions.Add(execution);

            task.LastRunAt = DateTime.UtcNow;
            task.LastStatus = TaskFlow.Shared.Models.TaskStatus.Running;
            task.NextRunAt = CalculateNextRun(task);
            task.ActiveExecutions = task.ActiveExecutions + 1;

            await db.SaveChangesAsync();

            await _kafka.PublishTriggerAsync(task, execution.Id);

            _logger.LogInformation(
                "[TRIGGERED] {Name} | Priority:{Priority} | Active:{Active}/{Max}",
                task.Name, task.Priority, task.ActiveExecutions, task.MaxConcurrent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ERROR] {Name} trigger hatası", task.Name);
            await _lock.ReleaseLockAsync(task.Id);
            _globalSemaphore.Release();
        }
        finally
        {
            _globalSemaphore.Release();
        }
    }

    // Missed run'ları recover et — uygulama başlarken çalışır
    private async Task RecoverMissedRunsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();

        var missed = await db.MissedRuns
            .Where(m => !m.Recovered)
            .Include(m => m.Task)
            .OrderBy(m => m.ScheduledAt)
            .ToListAsync();

        if (missed.Count == 0) return;

        _logger.LogInformation("[RECOVERY] {Count} missed run bulundu", missed.Count);

        foreach (var run in missed)
        {
            if (run.Task is null || !run.Task.IsEnabled) continue;

            _logger.LogInformation("[RECOVERY] {TaskName} çalıştırılıyor (missed: {At})",
                run.TaskName, run.ScheduledAt);

            await _kafka.PublishTriggerAsync(run.Task, Guid.NewGuid());

            run.Recovered = true;
            run.RecoveredAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
    }

    private DateTime? CalculateNextRun(ScheduledTask task)
    {
        return task.ScheduleType switch
        {
            ScheduleType.Cron => ParseCron(task.CronExpression),
            ScheduleType.Interval => DateTime.UtcNow.AddMinutes(task.IntervalMinutes ?? 10),
            _ => null
        };
    }

    private DateTime? ParseCron(string? expression)
    {
        if (string.IsNullOrEmpty(expression)) return null;
        try
        {
            var cron = CronExpression.Parse(expression);
            return cron.GetNextOccurrence(DateTime.UtcNow, TimeZoneInfo.Utc);
        }
        catch { return null; }
    }
}