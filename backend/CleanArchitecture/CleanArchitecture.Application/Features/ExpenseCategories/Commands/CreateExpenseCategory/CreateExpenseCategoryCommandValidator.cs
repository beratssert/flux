using FluentValidation;

namespace CleanArchitecture.Core.Features.ExpenseCategories.Commands.CreateExpenseCategory
{
    public class CreateExpenseCategoryCommandValidator : AbstractValidator<CreateExpenseCategoryCommand>
    {
        public CreateExpenseCategoryCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(150);
        }
    }
}
