using FluentValidation;

namespace CleanArchitecture.Core.Features.TimeEntries.Commands.UpdateTimeEntry
{
    public class UpdateTimeEntryCommandValidator : AbstractValidator<UpdateTimeEntryCommand>
    {
        public UpdateTimeEntryCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0);

            RuleFor(x => x.ProjectId)
                .GreaterThan(0);

            RuleFor(x => x.EntryDate)
                .NotEmpty();

            RuleFor(x => x.Description)
                .MaximumLength(1000);
        }
    }
}
