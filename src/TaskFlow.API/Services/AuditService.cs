using System.Text.Json;
using TaskFlow.API.Data;
using TaskFlow.Shared.Models;

namespace TaskFlow.API.Services;

public class AuditService
{
    private readonly TaskFlowDbContext _db;
    private readonly IHttpContextAccessor _httpContext;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        TaskFlowDbContext db,
        IHttpContextAccessor httpContext,
        ILogger<AuditService> logger)
    {
        _db = db;
        _httpContext = httpContext;
        _logger = logger;
    }

    public async Task LogAsync(
        string action,
        string entityType,
        Guid? entityId,
        string entityName,
        object? oldValue = null,
        object? newValue = null)
    {
        try
        {
            var log = new AuditLog
            {
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                EntityName = entityName,
                OldValue = oldValue is null ? null : JsonSerializer.Serialize(oldValue),
                NewValue = newValue is null ? null : JsonSerializer.Serialize(newValue),
                IpAddress = _httpContext.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            };

            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync();

            _logger.LogInformation("[AUDIT] {Action} | {EntityType}: {EntityName}",
                action, entityType, entityName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audit log yazılamadı");
        }
    }
}