using FluentValidation;

namespace CleanArchitecture.Core.Features.Expenses.Commands.UpdateExpense
{
    public class UpdateExpenseCommandValidator : AbstractValidator<UpdateExpenseCommand>
    {
        public UpdateExpenseCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
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
