using ChronicleHub.Api.Contracts.Events;
using FluentValidation;

namespace ChronicleHub.Api.Validators;

public class CreateEventRequestValidator : AbstractValidator<CreateEventRequest>
{
    private static readonly DateTime MaxAllowedFutureDate = DateTime.UtcNow.AddYears(1);

    public CreateEventRequestValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty()
            .WithMessage("Type is required and cannot be empty.");

        RuleFor(x => x.Source)
            .NotEmpty()
            .WithMessage("Source is required and cannot be empty.");

        RuleFor(x => x.TimestampUtc)
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .WithMessage("TimestampUtc cannot be more than 1 day in the future.");

        RuleFor(x => x.Payload)
            .Must(payload => payload.ValueKind != System.Text.Json.JsonValueKind.Null
                          && payload.ValueKind != System.Text.Json.JsonValueKind.Undefined)
            .WithMessage("Payload cannot be null or undefined.");
    }
}
