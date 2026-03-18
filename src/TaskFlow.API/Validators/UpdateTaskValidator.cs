using FluentValidation;
using TaskFlow.API.CQRS.Commands;
using TaskFlow.Shared.Models;

namespace TaskFlow.API.Validators;

public class UpdateTaskValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Task name is required")
            .MaximumLength(200);

        RuleFor(x => x.WebhookUrl)
            .NotEmpty()
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("Webhook URL must be a valid HTTP/HTTPS URL");

        RuleFor(x => x.RetryCount)
            .InclusiveBetween(0, 10);

        RuleFor(x => x.TimeoutSeconds)
            .InclusiveBetween(1, 3600);
    }
}