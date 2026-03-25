using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using TaskFlow.API.Data;
using TaskFlow.Shared.Models;

namespace TaskFlow.API.Services;

public class AlertService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<AlertService> _logger;

    public AlertService(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpFactory,
        ILogger<AlertService> logger)
    {
        _scopeFactory = scopeFactory;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public async Task CheckAndFireAsync(Guid taskId, bool isSuccess, bool isDead)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TaskFlowDbContext>();

        var rules = await db.AlertRules
            .Where(r => r.TaskId == taskId && r.IsEnabled)
            .ToListAsync();

        if (!rules.Any()) return;

        var task = await db.Tasks.FindAsync(taskId);
        if (task is null) return;

        // Art arda başarısız sayısını hesapla
        var recentExecs = await db.Executions
            .Where(e => e.TaskId == taskId)
            .OrderByDescending(e => e.StartedAt)
            .Take(10)
            .ToListAsync();

        var consecutiveFails = 0;
        foreach (var exec in recentExecs)
        {
            if (exec.Status == ExecutionStatus.Failed || exec.Status == ExecutionStatus.Dead)
                consecutiveFails++;
            else
                break;
        }

        foreach (var rule in rules)
        {
            var shouldFire = rule.TriggerType switch
            {
                AlertTriggerType.AnyFailure => !isSuccess,
                AlertTriggerType.TaskDead => isDead,
                AlertTriggerType.ConsecutiveFailures => consecutiveFails >= rule.Threshold,
                _ => false
            };

            if (!shouldFire) continue;

            // Son 5 dakikada aynı kural tetiklendiyse atla
            if (rule.LastTriggeredAt.HasValue &&
                DateTime.UtcNow - rule.LastTriggeredAt.Value < TimeSpan.FromMinutes(5))
                continue;

            await FireAlertAsync(db, rule, task, consecutiveFails);
        }

        await db.SaveChangesAsync();
    }

    private async Task FireAlertAsync(
        TaskFlowDbContext db,
        AlertRule rule,
        ScheduledTask task,
        int consecutiveFails)
    {
        var message = rule.TriggerType switch
        {
            AlertTriggerType.ConsecutiveFailures =>
                $"⚠️ Task '{task.Name}' failed {consecutiveFails} times consecutively",
            AlertTriggerType.TaskDead =>
                $"💀 Task '{task.Name}' is DEAD — max retries exceeded",
            AlertTriggerType.AnyFailure =>
                $"❌ Task '{task.Name}' failed",
            _ => $"Alert: Task '{task.Name}'"
        };

        var history = new AlertHistory
        {
            AlertRuleId = rule.Id,
            TaskId = task.Id,
            TaskName = task.Name,
            Message = message,
        };

        try
        {
            // Webhook payload oluştur
            var payload = rule.WebhookTemplate is not null
                ? rule.WebhookTemplate
                    .Replace("{{task}}", task.Name)
                    .Replace("{{message}}", message)
                    .Replace("{{time}}", DateTime.UtcNow.ToString("O"))
                : JsonSerializer.Serialize(new
                {
                    text = message,
                    task = task.Name,
                    taskId = task.Id,
                    time = DateTime.UtcNow,
                    rule = rule.Name,
                });

            using var client = _httpFactory.CreateClient();
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(rule.WebhookUrl, content);

            history.Delivered = response.IsSuccessStatusCode;
            if (!response.IsSuccessStatusCode)
                history.Error = $"HTTP {(int)response.StatusCode}";

            rule.LastTriggeredAt = DateTime.UtcNow;

            _logger.LogInformation("[ALERT] {RuleName} fired → {TaskName} | Delivered: {Delivered}",
                rule.Name, task.Name, history.Delivered);
        }
        catch (Exception ex)
        {
            history.Error = ex.Message;
            _logger.LogError(ex, "[ALERT ERROR] {RuleName}", rule.Name);
        }

        db.AlertHistories.Add(history);
    }
}