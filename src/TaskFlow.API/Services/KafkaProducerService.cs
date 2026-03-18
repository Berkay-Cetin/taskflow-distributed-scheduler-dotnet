using Confluent.Kafka;
using TaskFlow.Shared.Messages;
using TaskFlow.Shared.Models;
using System.Text.Json;

namespace TaskFlow.API.Services;

public class KafkaProducerService : IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly string _triggerTopic;
    private readonly ILogger<KafkaProducerService> _logger;

    public KafkaProducerService(IConfiguration config, ILogger<KafkaProducerService> logger)
    {
        _logger = logger;
        _triggerTopic = config["Kafka:Topics:TaskTrigger"]!;

        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"],
            Acks = Acks.All,
            EnableIdempotence = true,
        }).Build();
    }

    public async Task PublishTriggerAsync(ScheduledTask task)
    {
        var message = new TaskTriggerMessage
        {
            TaskId = task.Id,
            TaskName = task.Name,
            WebhookUrl = task.WebhookUrl,
            HttpMethod = task.HttpMethod,
            Headers = task.WebhookHeaders,
            Body = task.WebhookBody,
            RetryCount = task.RetryCount,
            RetryDelay = task.RetryDelaySeconds,
            Timeout = task.TimeoutSeconds,
        };

        var result = await _producer.ProduceAsync(_triggerTopic, new Message<string, string>
        {
            Key = task.Id.ToString(),
            Value = JsonSerializer.Serialize(message),
            Headers = new Headers
            {
                { "event-type", "TaskTrigger"u8.ToArray() },
                { "source",     "TaskFlow.API"u8.ToArray() }
            }
        });

        _logger.LogInformation("[KAFKA] TaskTrigger published → {Name} | P:{P} O:{O}",
            task.Name, result.Partition.Value, result.Offset.Value);
    }

    public void Dispose() => _producer?.Dispose();
}