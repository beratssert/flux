using FluentValidation;

namespace CleanArchitecture.Core.Features.Expenses.Commands.CreateExpense
{
    public class CreateExpenseCommandValidator : AbstractValidator<CreateExpenseCommand>
    {
        public CreateExpenseCommandValidator()
        {
            RuleFor(x => x.ProjectId).GreaterThan(0);
            RuleFor(x => x.CategoryId).GreaterThan(0);
            RuleFor(x => x.ExpenseDate).NotEmpty();
            RuleFor(x => x.Amount).GreaterThan(0);
            RuleFor(x => x.CurrencyCode)
                .NotEmpty()
                .Length(3)
                .Matches("^[A-Za-z]{3}$");
            RuleFor(x => x.Notes).MaximumLength(1000);
            RuleFor(x => x.ReceiptUrl).MaximumLength(1000);
        }
    }
}
