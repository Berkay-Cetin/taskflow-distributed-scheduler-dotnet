using StackExchange.Redis;

namespace TaskFlow.Scheduler.Services;

public class RedisLockService
{
    private readonly IDatabase _redis;
    private const string LOCK_PREFIX = "taskflow:lock:";

    public RedisLockService(IConnectionMultiplexer redis)
    {
        _redis = redis.GetDatabase();
    }

    // Distributed lock — aynı task aynı anda sadece 1 kez çalışsın
    public async Task<bool> AcquireLockAsync(Guid taskId, int durationSeconds = 300)
    {
        var key = $"{LOCK_PREFIX}{taskId}";
        var value = Environment.MachineName; // hangi instance aldı

        // SET key value NX EX seconds — sadece key yoksa set et
        return await _redis.StringSetAsync(
            key,
            value,
            TimeSpan.FromSeconds(durationSeconds),
            When.NotExists);
    }

    public async Task ReleaseLockAsync(Guid taskId)
    {
        var key = $"{LOCK_PREFIX}{taskId}";
        await _redis.KeyDeleteAsync(key);
    }

    public async Task<bool> IsLockedAsync(Guid taskId)
    {
        var key = $"{LOCK_PREFIX}{taskId}";
        return await _redis.KeyExistsAsync(key);
    }
}