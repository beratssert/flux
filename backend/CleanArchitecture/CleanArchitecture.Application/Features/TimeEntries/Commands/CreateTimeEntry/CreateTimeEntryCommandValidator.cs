using FluentValidation;

namespace CleanArchitecture.Core.Features.TimeEntries.Commands.CreateTimeEntry
{
    public class CreateTimeEntryCommandValidator : AbstractValidator<CreateTimeEntryCommand>
    {
        public CreateTimeEntryCommandValidator()
        {
            RuleFor(x => x.ProjectId)
                .GreaterThan(0);

            RuleFor(x => x.EntryDate)
                .NotEmpty();

            RuleFor(x => x.Description)
                .MaximumLength(1000);
        }
    }
}
