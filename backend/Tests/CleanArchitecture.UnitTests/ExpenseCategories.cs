using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Features.ExpenseCategories.Commands.CreateExpenseCategory;
using CleanArchitecture.Core.Features.ExpenseCategories.Commands.UpdateExpenseCategory;
using CleanArchitecture.Core.Interfaces.Repositories;
using Moq;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.UnitTests
{
    public class ExpenseCategories
    {
        private readonly Mock<IExpenseCategoryRepositoryAsync> _repo = new();

        [Fact]
        public async Task CreateExpenseCategory_WhenNameExists_ShouldThrowApiException()
        {
            _repo.Setup(r => r.GetByNameAsync("Travel")).ReturnsAsync(new ExpenseCategory { Id = 5, Name = "Travel" });
            var handler = new CreateExpenseCategoryCommandHandler(_repo.Object);

            await Assert.ThrowsAsync<ApiException>(() =>
                handler.Handle(new CreateExpenseCategoryCommand { Name = "Travel" }, CancellationToken.None));
        }

        [Fact]
        public async Task UpdateExpenseCategory_WhenMissing_ShouldThrowApiException()
        {
            _repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((ExpenseCategory)null);
            var handler = new UpdateExpenseCategoryCommandHandler(_repo.Object);

            await Assert.ThrowsAsync<ApiException>(() =>
                handler.Handle(new UpdateExpenseCategoryCommand { Id = 99, Name = "Meals", IsActive = true }, CancellationToken.None));
        }
    }
}
