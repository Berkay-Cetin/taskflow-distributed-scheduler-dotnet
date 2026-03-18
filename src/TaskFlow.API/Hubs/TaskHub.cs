using Microsoft.AspNetCore.SignalR;

namespace TaskFlow.API.Hubs;

public class TaskHub : Hub
{
    // Client method'ları — React bu isimleri dinleyecek
    // "TaskStatusUpdated" — task durumu değişti
    // "ExecutionStarted"  — execution başladı
    // "ExecutionCompleted"— execution tamamlandı
    // "ExecutionFailed"   — execution başarısız

    public async Task JoinTaskGroup(string taskId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"task-{taskId}");
    }

    public async Task LeaveTaskGroup(string taskId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"task-{taskId}");
    }
}