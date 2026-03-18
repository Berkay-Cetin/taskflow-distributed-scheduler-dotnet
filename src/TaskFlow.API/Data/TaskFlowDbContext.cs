using Microsoft.EntityFrameworkCore;
using TaskFlow.Shared.Models;

namespace TaskFlow.API.Data;

public class TaskFlowDbContext : DbContext
{
    public TaskFlowDbContext(DbContextOptions<TaskFlowDbContext> options) : base(options) { }

    public DbSet<ScheduledTask> Tasks => Set<ScheduledTask>();
    public DbSet<TaskExecution> Executions => Set<TaskExecution>();
    public DbSet<MissedRun> MissedRuns => Set<MissedRun>();
    public DbSet<TaskTag> Tags => Set<TaskTag>();
    public DbSet<ScheduledTaskTag> TaskTags => Set<ScheduledTaskTag>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ExecutionLog> ExecutionLogs => Set<ExecutionLog>();

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

        mb.Entity<TaskTag>(e =>
        {
            e.ToTable("task_tags");
            e.HasKey(t => t.Id);
            e.Property(t => t.Name).HasMaxLength(50);
            e.Property(t => t.Color).HasMaxLength(7);
            e.HasIndex(t => t.Name).IsUnique();
        });

        mb.Entity<ScheduledTaskTag>(e =>
        {
            e.ToTable("scheduled_task_tags");
            e.HasKey(x => new { x.TaskId, x.TagId });

            e.HasOne(x => x.Task)
             .WithMany(t => t.TaskTags)
             .HasForeignKey(x => x.TaskId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Tag)
             .WithMany(t => t.TaskTags)
             .HasForeignKey(x => x.TagId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<AuditLog>(e =>
        {
            e.ToTable("audit_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).HasMaxLength(100);
            e.Property(x => x.EntityType).HasMaxLength(50);
            e.Property(x => x.EntityName).HasMaxLength(200);
            e.HasIndex(x => x.EntityId);
            e.HasIndex(x => x.Action);
            e.HasIndex(x => x.CreatedAt);
        });

        mb.Entity<ExecutionLog>(e =>
        {
            e.ToTable("execution_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Level).HasConversion<string>();
            e.Property(x => x.Message).HasMaxLength(500);
            e.HasOne(x => x.Execution)
             .WithMany(x => x.Logs)
             .HasForeignKey(x => x.ExecutionId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.ExecutionId);
            e.HasIndex(x => x.CreatedAt);
        });
    }
}