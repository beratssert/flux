using FluentValidation;

namespace CleanArchitecture.Core.Features.Expenses.Commands.SubmitExpense
{
    public class SubmitExpenseCommandValidator : AbstractValidator<SubmitExpenseCommand>
    {
        public SubmitExpenseCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
