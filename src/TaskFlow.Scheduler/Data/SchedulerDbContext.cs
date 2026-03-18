using Microsoft.EntityFrameworkCore;
using TaskFlow.Shared.Models;

namespace TaskFlow.Scheduler.Data;

public class SchedulerDbContext : DbContext
{
    public SchedulerDbContext(DbContextOptions<SchedulerDbContext> options) : base(options) { }

    public DbSet<ScheduledTask> Tasks => Set<ScheduledTask>();
    public DbSet<TaskExecution> Executions => Set<TaskExecution>();
    public DbSet<MissedRun> MissedRuns => Set<MissedRun>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<ScheduledTask>(e =>
        {
            e.ToTable("scheduled_tasks");
            e.HasKey(t => t.Id);
            e.Property(t => t.ScheduleType).HasConversion<string>();
            e.Property(t => t.LastStatus).HasConversion<string>();
            e.Ignore(t => t.TaskTags);  // Scheduler'da tag'lere gerek yok
        });

        mb.Entity<TaskExecution>(e =>
        {
            e.ToTable("task_executions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<string>();
            e.Ignore(x => x.Logs);
        });

        mb.Entity<MissedRun>(e =>
        {
            e.ToTable("missed_runs");
            e.HasKey(x => x.Id);
            e.Ignore(x => x.Task);
        });
    }
}