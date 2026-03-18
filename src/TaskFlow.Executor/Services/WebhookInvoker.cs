using System.Diagnostics;
using System.Text;
using System.Text.Json;
using TaskFlow.Shared.Messages;

namespace TaskFlow.Executor.Services;

public class WebhookInvoker
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<WebhookInvoker> _logger;

    public WebhookInvoker(IHttpClientFactory httpFactory, ILogger<WebhookInvoker> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public async Task<TaskResultMessage> InvokeAsync(TaskTriggerMessage trigger)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            using var client = _httpFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(trigger.Timeout > 0 ? trigger.Timeout : 60);

            // Custom headers
            if (!string.IsNullOrEmpty(trigger.Headers))
            {
                var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(trigger.Headers);
                if (headers is not null)
                    foreach (var (key, value) in headers)
                        client.DefaultRequestHeaders.TryAddWithoutValidation(key, value);
            }

            // HTTP isteği oluştur
            var request = new HttpRequestMessage(
                new HttpMethod(trigger.HttpMethod),
                trigger.WebhookUrl);

            if (!string.IsNullOrEmpty(trigger.Body))
                request.Content = new StringContent(trigger.Body, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);
            sw.Stop();

            var responseBody = await response.Content.ReadAsStringAsync();
            var isSuccess = response.IsSuccessStatusCode;

            _logger.LogInformation(
                "[WEBHOOK] {TaskName} → {StatusCode} | {Duration}ms | Attempt: {Attempt}",
                trigger.TaskName, (int)response.StatusCode, sw.ElapsedMilliseconds, trigger.AttemptNo);

            return new TaskResultMessage
            {
                TaskId = trigger.TaskId,
                ExecutionId = trigger.ExecutionId ?? Guid.NewGuid(),
                IsSuccess = isSuccess,
                StatusCode = (int)response.StatusCode,
                Response = responseBody[..Math.Min(responseBody.Length, 2000)],
                DurationMs = sw.ElapsedMilliseconds,
                AttemptNo = trigger.AttemptNo,
                WillRetry = !isSuccess && trigger.AttemptNo < trigger.RetryCount,
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[WEBHOOK ERROR] {TaskName} | Attempt: {Attempt}",
                trigger.TaskName, trigger.AttemptNo);

            return new TaskResultMessage
            {
                TaskId = trigger.TaskId,
                ExecutionId = trigger.ExecutionId ?? Guid.NewGuid(),
                IsSuccess = false,
                ErrorMessage = ex.Message,
                DurationMs = sw.ElapsedMilliseconds,
                AttemptNo = trigger.AttemptNo,
                WillRetry = trigger.AttemptNo < trigger.RetryCount,
            };
        }
    }
}