using FluentValidation;

namespace CleanArchitecture.Core.Features.Expenses.Commands.RejectExpense
{
    public class RejectExpenseCommandValidator : AbstractValidator<RejectExpenseCommand>
    {
        public RejectExpenseCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.Reason)
                .NotEmpty()
                .MaximumLength(1000);
        }
    }
}
