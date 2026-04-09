using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.Enums;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Features.Expenses.Commands.CreateExpense;
using CleanArchitecture.Core.Features.Expenses.Commands.RejectExpense;
using CleanArchitecture.Core.Features.Expenses.Commands.UpdateExpense;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.UnitTests
{
    public class Expenses
    {
        private readonly Mock<IExpenseRepositoryAsync> _expenseRepository = new();
        private readonly Mock<IProjectAssignmentRepositoryAsync> _projectAssignmentRepository = new();
        private readonly Mock<IAuthenticatedUserService> _authenticatedUserService = new();
        private readonly Mock<IAuditService> _auditService = new();

        public Expenses()
        {
            _authenticatedUserService.SetupGet(a => a.UserId).Returns("employee-1");
            _authenticatedUserService.SetupGet(a => a.Role).Returns("Employee");
            _auditService.Setup(a => a.WriteAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>())).Returns(Task.CompletedTask);
        }

        [Fact]
        public async Task CreateExpense_WhenNotAssigned_ShouldThrowApiException()
        {
            _projectAssignmentRepository.Setup(r => r.IsUserAssignedToProjectAsync("employee-1", 10)).ReturnsAsync(false);
            var handler = new CreateExpenseCommandHandler(
                _expenseRepository.Object,
                _projectAssignmentRepository.Object,
                _authenticatedUserService.Object,
                _auditService.Object);

            await Assert.ThrowsAsync<ApiException>(() => handler.Handle(new CreateExpenseCommand
            {
                ProjectId = 10,
                CategoryId = 1,
                ExpenseDate = DateTime.UtcNow,
                Amount = 100,
                CurrencyCode = "TRY"
            }, CancellationToken.None));
        }

        [Fact]
        public async Task UpdateExpense_WhenSubmitted_ShouldThrowApiException()
        {
            _expenseRepository.Setup(r => r.GetByIdAndUserIdAsync(1, "employee-1")).ReturnsAsync(new Expense
            {
                Id = 1,
                UserId = "employee-1",
                ProjectId = 10,
                CategoryId = 1,
                Status = ExpenseStatuses.Submitted
            });

            var handler = new UpdateExpenseCommandHandler(
                _expenseRepository.Object,
                _projectAssignmentRepository.Object,
                _authenticatedUserService.Object,
                _auditService.Object);

            await Assert.ThrowsAsync<ApiException>(() => handler.Handle(new UpdateExpenseCommand
            {
                Id = 1,
                ProjectId = 10,
                CategoryId = 1,
                ExpenseDate = DateTime.UtcNow,
                Amount = 120,
                CurrencyCode = "TRY"
            }, CancellationToken.None));
        }

        [Fact]
        public async Task RejectExpense_WhenManagerAndSubmitted_ShouldReject()
        {
            _authenticatedUserService.SetupGet(a => a.UserId).Returns("manager-1");
            _authenticatedUserService.SetupGet(a => a.Role).Returns("Manager");
            _expenseRepository.Setup(r => r.GetByIdInManagerScopeAsync(44, "manager-1")).ReturnsAsync(new Expense
            {
                Id = 44,
                UserId = "employee-2",
                Status = ExpenseStatuses.Submitted
            });

            var handler = new RejectExpenseCommandHandler(
                _expenseRepository.Object,
                _authenticatedUserService.Object,
                _auditService.Object);

            var id = await handler.Handle(new RejectExpenseCommand { Id = 44, Reason = "Receipt unreadable" }, CancellationToken.None);

            Assert.Equal(44, id);
            _expenseRepository.Verify(r => r.UpdateAsync(It.Is<Expense>(e =>
                e.Status == ExpenseStatuses.Rejected &&
                e.RejectionReason == "Receipt unreadable" &&
                e.ReviewedByUserId == "manager-1")), Times.Once);
        }
    }
}
