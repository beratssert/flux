using CleanArchitecture.Core.DTOs.Expenses;
using CleanArchitecture.Core.Enums;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Features.Reports.Queries.GetManagerTeamExpenseSummary;
using CleanArchitecture.Core.Features.Reports.Queries.GetMyExpenseSummary;
using CleanArchitecture.Core.Features.Reports.Queries.GetProjectSummary;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CleanArchitecture.UnitTests;

public class ReportsExtendedTests
{
    private readonly Mock<ITimeEntryRepositoryAsync> _timeEntries = new();
    private readonly Mock<IExpenseRepositoryAsync> _expenses = new();
    private readonly Mock<IAuthenticatedUserService> _auth = new();

    [Fact]
    public async Task GetProjectSummary_WhenToBeforeFrom_ShouldThrowApiException()
    {
        _auth.SetupGet(a => a.Role).Returns(Roles.Admin.ToString());
        var handler = new GetProjectSummaryQueryHandler(_timeEntries.Object, _expenses.Object, _auth.Object);

        await Assert.ThrowsAsync<ApiException>(() =>
            handler.Handle(
                new GetProjectSummaryQuery
                {
                    ProjectId = 1,
                    From = new DateTime(2026, 6, 10),
                    To = new DateTime(2026, 6, 1)
                },
                CancellationToken.None));
    }

    [Fact]
    public async Task GetProjectSummary_WhenProjectMissing_ShouldThrowNotFound()
    {
        _auth.SetupGet(a => a.Role).Returns(Roles.Admin.ToString());
        _timeEntries.Setup(t => t.ProjectExistsAsync(99)).ReturnsAsync(false);

        var handler = new GetProjectSummaryQueryHandler(_timeEntries.Object, _expenses.Object, _auth.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetProjectSummaryQuery { ProjectId = 99 }, CancellationToken.None));
    }

    [Fact]
    public async Task GetProjectSummary_WhenManagerNotManaging_ShouldThrowNotFound()
    {
        _auth.SetupGet(a => a.UserId).Returns("mgr-1");
        _auth.SetupGet(a => a.Role).Returns(Roles.Manager.ToString());
        _timeEntries.Setup(t => t.ProjectExistsAsync(1)).ReturnsAsync(true);
        _timeEntries.Setup(t => t.IsProjectManagedByAsync("mgr-1", 1)).ReturnsAsync(false);

        var handler = new GetProjectSummaryQueryHandler(_timeEntries.Object, _expenses.Object, _auth.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetProjectSummaryQuery { ProjectId = 1 }, CancellationToken.None));
    }

    [Fact]
    public async Task GetProjectSummary_WhenAdmin_ShouldAggregateTimeAndExpense()
    {
        _auth.SetupGet(a => a.Role).Returns(Roles.Admin.ToString());
        _timeEntries.Setup(t => t.ProjectExistsAsync(5)).ReturnsAsync(true);
        _timeEntries.Setup(t => t.GetProjectAggregateAllAsync(5, null, null)).ReturnsAsync((600, 10, 4));
        _expenses.Setup(e => e.GetProjectTotalAmountAllAsync(5, null, null)).ReturnsAsync(250.50m);

        var handler = new GetProjectSummaryQueryHandler(_timeEntries.Object, _expenses.Object, _auth.Object);
        var result = await handler.Handle(new GetProjectSummaryQuery { ProjectId = 5 }, CancellationToken.None);

        Assert.Equal(5, result.ProjectId);
        Assert.Equal(600, result.TotalMinutes);
        Assert.Equal(250.50m, result.TotalExpenseAmount);
        Assert.Equal(40m, result.BillableEntryRate);
    }

    [Fact]
    public async Task GetProjectSummary_WhenManagerManages_ShouldUseScopedRepositories()
    {
        _auth.SetupGet(a => a.UserId).Returns("mgr-1");
        _auth.SetupGet(a => a.Role).Returns(Roles.Manager.ToString());
        _timeEntries.Setup(t => t.ProjectExistsAsync(3)).ReturnsAsync(true);
        _timeEntries.Setup(t => t.IsProjectManagedByAsync("mgr-1", 3)).ReturnsAsync(true);
        _timeEntries.Setup(t => t.GetProjectAggregateByManagedProjectsAsync("mgr-1", 3, null, null)).ReturnsAsync((120, 5, 5));
        _expenses.Setup(e => e.GetProjectTotalAmountByManagedProjectsAsync("mgr-1", 3, null, null)).ReturnsAsync(10m);

        var handler = new GetProjectSummaryQueryHandler(_timeEntries.Object, _expenses.Object, _auth.Object);
        var result = await handler.Handle(new GetProjectSummaryQuery { ProjectId = 3 }, CancellationToken.None);

        Assert.Equal(120, result.TotalMinutes);
        Assert.Equal(10m, result.TotalExpenseAmount);
        Assert.Equal(100m, result.BillableEntryRate);
        _timeEntries.Verify(
            t => t.GetProjectAggregateAllAsync(It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()),
            Times.Never);
    }

    [Fact]
    public async Task GetProjectSummary_WhenAdminWithDateRange_PassesFromToToRepositories()
    {
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 1, 31);
        _auth.SetupGet(a => a.Role).Returns(Roles.Admin.ToString());
        _timeEntries.Setup(t => t.ProjectExistsAsync(7)).ReturnsAsync(true);
        _timeEntries.Setup(t => t.GetProjectAggregateAllAsync(7, from, to)).ReturnsAsync((100, 2, 1));
        _expenses.Setup(e => e.GetProjectTotalAmountAllAsync(7, from, to)).ReturnsAsync(99m);

        var handler = new GetProjectSummaryQueryHandler(_timeEntries.Object, _expenses.Object, _auth.Object);
        await handler.Handle(
            new GetProjectSummaryQuery { ProjectId = 7, From = from, To = to },
            CancellationToken.None);

        _timeEntries.Verify(t => t.GetProjectAggregateAllAsync(7, from, to), Times.Once);
        _expenses.Verify(e => e.GetProjectTotalAmountAllAsync(7, from, to), Times.Once);
    }

    [Fact]
    public async Task GetMyExpenseSummary_WhenGroupedByProject_ShouldAggregate()
    {
        _auth.SetupGet(a => a.UserId).Returns("u1");
        _expenses
            .Setup(e => e.GetSummaryRowsByUserAsync("u1", null, null, null))
            .ReturnsAsync(new List<ExpenseSummaryRowDto>
            {
                new()
                {
                    UserId = "u1",
                    ProjectId = 10,
                    CategoryId = 1,
                    ExpenseDate = new DateTime(2026, 4, 1),
                    Amount = 50m,
                    CurrencyCode = "USD"
                },
                new()
                {
                    UserId = "u1",
                    ProjectId = 10,
                    CategoryId = 2,
                    ExpenseDate = new DateTime(2026, 4, 2),
                    Amount = 25m,
                    CurrencyCode = "USD"
                }
            });

        var handler = new GetMyExpenseSummaryQueryHandler(_expenses.Object, _auth.Object);
        var result = await handler.Handle(
            new GetMyExpenseSummaryQuery { GroupBy = "project" },
            CancellationToken.None);

        Assert.Equal(75m, result.TotalAmount);
        Assert.Single(result.Groups);
        Assert.Equal("10", result.Groups[0].Key);
        Assert.Equal(75m, result.Groups[0].Amount);
    }

    [Fact]
    public async Task GetMyExpenseSummary_WhenInvalidGroupBy_ShouldThrow()
    {
        _auth.SetupGet(a => a.UserId).Returns("u1");
        var handler = new GetMyExpenseSummaryQueryHandler(_expenses.Object, _auth.Object);

        await Assert.ThrowsAsync<ApiException>(() =>
            handler.Handle(new GetMyExpenseSummaryQuery { GroupBy = "user" }, CancellationToken.None));
    }

    [Fact]
    public async Task GetManagerTeamExpenseSummary_WhenAdmin_ShouldCallAllScope()
    {
        _auth.SetupGet(a => a.Role).Returns(Roles.Admin.ToString());
        _expenses
            .Setup(e => e.GetSummaryRowsAllAsync(2, "emp-1", null, null, null, null))
            .ReturnsAsync(new List<ExpenseSummaryRowDto>
            {
                new()
                {
                    UserId = "emp-1",
                    ProjectId = 2,
                    CategoryId = 1,
                    ExpenseDate = new DateTime(2026, 4, 1),
                    Amount = 100m,
                    CurrencyCode = "USD"
                }
            });

        var handler = new GetManagerTeamExpenseSummaryQueryHandler(_expenses.Object, _auth.Object);
        var result = await handler.Handle(
            new GetManagerTeamExpenseSummaryQuery
            {
                ProjectId = 2,
                UserId = "emp-1",
                GroupBy = "user"
            },
            CancellationToken.None);

        Assert.Equal(100m, result.TotalAmount);
        _expenses.Verify(
            e => e.GetSummaryRowsAllAsync(2, "emp-1", null, null, null, null),
            Times.Once);
        _expenses.Verify(
            e => e.GetSummaryRowsByManagedProjectsAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task GetManagerTeamExpenseSummary_WhenManager_ShouldCallManagedScope()
    {
        _auth.SetupGet(a => a.UserId).Returns("mgr-1");
        _auth.SetupGet(a => a.Role).Returns(Roles.Manager.ToString());
        _expenses
            .Setup(e => e.GetSummaryRowsByManagedProjectsAsync("mgr-1", null, null, null, null, null, null))
            .ReturnsAsync(Array.Empty<ExpenseSummaryRowDto>());

        var handler = new GetManagerTeamExpenseSummaryQueryHandler(_expenses.Object, _auth.Object);
        await handler.Handle(new GetManagerTeamExpenseSummaryQuery { GroupBy = "month" }, CancellationToken.None);

        _expenses.Verify(
            e => e.GetSummaryRowsByManagedProjectsAsync("mgr-1", null, null, null, null, null, null),
            Times.Once);
    }

    [Fact]
    public async Task GetManagerTeamExpenseSummary_WhenInvalidGroupBy_ShouldThrow()
    {
        _auth.SetupGet(a => a.Role).Returns(Roles.Manager.ToString());
        _auth.SetupGet(a => a.UserId).Returns("mgr-1");

        var handler = new GetManagerTeamExpenseSummaryQueryHandler(_expenses.Object, _auth.Object);

        await Assert.ThrowsAsync<ApiException>(() =>
            handler.Handle(new GetManagerTeamExpenseSummaryQuery { GroupBy = "category" }, CancellationToken.None));
    }
}
