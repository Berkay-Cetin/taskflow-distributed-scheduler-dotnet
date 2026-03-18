using Microsoft.EntityFrameworkCore;
using TaskFlow.Shared.Models;

namespace TaskFlow.API.Data;

public class TaskFlowDbContext : DbContext
{
    public TaskFlowDbContext(DbContextOptions<TaskFlowDbContext> options) : base(options) { }

    public DbSet<ScheduledTask> Tasks => Set<ScheduledTask>();
    public DbSet<TaskExecution> Executions => Set<TaskExecution>();
    public DbSet<MissedRun> MissedRuns => Set<MissedRun>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<ScheduledTask>(e =>
        {
            e.ToTable("scheduled_tasks");
            e.HasKey(t => t.Id);
            e.Property(t => t.Name).HasMaxLength(200);
            e.Property(t => t.WebhookUrl).HasMaxLength(500);
            e.Property(t => t.HttpMethod).HasMaxLength(10);
            e.Property(t => t.ScheduleType).HasConversion<string>();
            e.Property(t => t.LastStatus).HasConversion<string>();
            e.HasIndex(t => t.IsEnabled);
            e.HasIndex(t => t.NextRunAt);
            e.HasIndex(t => t.Priority);
        });

        mb.Entity<TaskExecution>(e =>
        {
            e.ToTable("task_executions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<string>();
            e.HasOne(x => x.Task)
             .WithMany()
             .HasForeignKey(x => x.TaskId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.TaskId);
            e.HasIndex(x => x.StartedAt);
            e.HasIndex(x => x.Status);
        });

        mb.Entity<MissedRun>(e =>
        {
            e.ToTable("missed_runs");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Task)
             .WithMany()
             .HasForeignKey(x => x.TaskId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.TaskId);
            e.HasIndex(x => x.Recovered);
            e.HasIndex(x => x.ScheduledAt);
        });
    }
}