using FluentValidation;

namespace CleanArchitecture.Core.Features.Timers.Commands.StartTimer
{
    public class StartTimerCommandValidator : AbstractValidator<StartTimerCommand>
    {
        public StartTimerCommandValidator()
        {
            RuleFor(x => x.ProjectId)
                .GreaterThan(0);

            RuleFor(x => x.Description)
                .MaximumLength(1000);
        }
    }
}
