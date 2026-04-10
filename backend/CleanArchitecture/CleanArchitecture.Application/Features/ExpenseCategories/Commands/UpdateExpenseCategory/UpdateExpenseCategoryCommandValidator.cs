using FluentValidation;

namespace CleanArchitecture.Core.Features.ExpenseCategories.Commands.UpdateExpenseCategory
{
    public class UpdateExpenseCategoryCommandValidator : AbstractValidator<UpdateExpenseCategoryCommand>
    {
        public UpdateExpenseCategoryCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(150);
        }
    }
}
