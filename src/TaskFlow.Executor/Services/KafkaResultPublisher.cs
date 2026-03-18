using Confluent.Kafka;
using TaskFlow.Shared.Messages;
using System.Text.Json;

namespace TaskFlow.Executor.Services;

public class KafkaResultPublisher : IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly string _resultTopic;
    private readonly string _deadLetterTopic;
    private readonly ILogger<KafkaResultPublisher> _logger;

    public KafkaResultPublisher(IConfiguration config, ILogger<KafkaResultPublisher> logger)
    {
        _logger = logger;
        _resultTopic = config["Kafka:Topics:TaskResult"]!;
        _deadLetterTopic = config["Kafka:Topics:DeadLetter"]!;

        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"],
            Acks = Acks.All,
            EnableIdempotence = true,
        }).Build();
    }

    public async Task PublishAsync(TaskResultMessage result)
    {
        var topic = result.IsSuccess || !result.WillRetry
            ? _resultTopic
            : _resultTopic;

        await _producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = result.TaskId.ToString(),
            Value = JsonSerializer.Serialize(result),
            Headers = new Headers
            {
                { "event-type", "TaskResult"u8.ToArray() },
                { "source",     "TaskFlow.Executor"u8.ToArray() }
            }
        });

        // Dead letter — max retry aşıldı
        if (!result.IsSuccess && !result.WillRetry)
        {
            await _producer.ProduceAsync(_deadLetterTopic, new Message<string, string>
            {
                Key = result.TaskId.ToString(),
                Value = JsonSerializer.Serialize(result),
            });
            _logger.LogWarning("[DLQ] Task {TaskId} max retry aştı", result.TaskId);
        }
    }

    public void Dispose() => _producer?.Dispose();
}