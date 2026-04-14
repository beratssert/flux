using FluentValidation;

namespace CleanArchitecture.Core.Features.CalendarEvents.Commands.CreateCalendarEvent
{
    public class CreateCalendarEventCommandValidator : AbstractValidator<CreateCalendarEventCommand>
    {
        public CreateCalendarEventCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .MaximumLength(200);
            RuleFor(x => x.Description)
                .MaximumLength(4000)
                .When(x => x.Description != null);
            RuleFor(x => x.VisibilityType)
                .NotEmpty();
            RuleFor(x => x)
                .Must(x => x.EndAtUtc > x.StartAtUtc)
                .WithMessage("endAtUtc must be after startAtUtc.");
        }
    }
}
