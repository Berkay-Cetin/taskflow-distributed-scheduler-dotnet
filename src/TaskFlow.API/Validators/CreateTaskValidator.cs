using FluentValidation;
using TaskFlow.API.CQRS.Commands;
using TaskFlow.Shared.Models;

namespace TaskFlow.API.Validators;

public class CreateTaskValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Task name is required")
            .MaximumLength(200).WithMessage("Task name must be at most 200 characters");

        RuleFor(x => x.WebhookUrl)
            .NotEmpty().WithMessage("Webhook URL is required")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("Webhook URL must be a valid HTTP/HTTPS URL");

        RuleFor(x => x.HttpMethod)
            .NotEmpty()
            .Must(m => new[] { "GET", "POST", "PUT", "PATCH", "DELETE" }.Contains(m.ToUpper()))
            .WithMessage("HTTP method must be GET, POST, PUT, PATCH or DELETE");

        RuleFor(x => x.CronExpression)
            .NotEmpty().When(x => x.ScheduleType == ScheduleType.Cron)
            .WithMessage("Cron expression is required for Cron schedule type")
            .Must(BeValidCronExpression).When(x => x.ScheduleType == ScheduleType.Cron)
            .WithMessage("Invalid cron expression");

        RuleFor(x => x.IntervalMinutes)
            .NotNull().When(x => x.ScheduleType == ScheduleType.Interval)
            .WithMessage("Interval minutes is required for Interval schedule type")
            .GreaterThan(0).When(x => x.IntervalMinutes.HasValue)
            .WithMessage("Interval must be greater than 0");

        RuleFor(x => x.RetryCount)
            .InclusiveBetween(0, 10)
            .WithMessage("Retry count must be between 0 and 10");

        RuleFor(x => x.RetryDelaySeconds)
            .InclusiveBetween(1, 3600)
            .WithMessage("Retry delay must be between 1 and 3600 seconds");

        RuleFor(x => x.TimeoutSeconds)
            .InclusiveBetween(1, 3600)
            .WithMessage("Timeout must be between 1 and 3600 seconds");

        RuleFor(x => x.MaxConcurrent)
            .InclusiveBetween(1, 10)
            .WithMessage("Max concurrent must be between 1 and 10");

        RuleFor(x => x.Priority)
            .InclusiveBetween(1, 10)
            .WithMessage("Priority must be between 1 and 10");

        RuleFor(x => x.WebhookBody)
            .Must(BeValidJson).When(x => !string.IsNullOrEmpty(x.WebhookBody))
            .WithMessage("Webhook body must be valid JSON");

        RuleFor(x => x.WebhookHeaders)
            .Must(BeValidJson).When(x => !string.IsNullOrEmpty(x.WebhookHeaders))
            .WithMessage("Webhook headers must be valid JSON");
    }

    private bool BeValidCronExpression(string? expression)
    {
        if (string.IsNullOrEmpty(expression)) return false;
        try
        {
            Cronos.CronExpression.Parse(expression);
            return true;
        }
        catch { return false; }
    }

    private bool BeValidJson(string? json)
    {
        if (string.IsNullOrEmpty(json)) return true;
        try
        {
            System.Text.Json.JsonDocument.Parse(json);
            return true;
        }
        catch { return false; }
    }
}