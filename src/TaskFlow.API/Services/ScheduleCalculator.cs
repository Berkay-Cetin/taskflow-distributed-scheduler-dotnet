using Cronos;
using TaskFlow.Shared.Models;

namespace TaskFlow.API.Services;

public class ScheduleCalculator
{
    public DateTime? GetNextRun(ScheduleType type, string? cronExpression, int? intervalMinutes)
    {
        return type switch
        {
            ScheduleType.Cron => GetNextCronRun(cronExpression),
            ScheduleType.Interval => DateTime.UtcNow.AddMinutes(intervalMinutes ?? 10),
            ScheduleType.Manual => null,
            _ => null
        };
    }

    private DateTime? GetNextCronRun(string? expression)
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