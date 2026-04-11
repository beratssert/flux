using AutoFixture;
using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Infrastructure.Contexts;
using CleanArchitecture.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Tests
{
    public class TimeTrackerRepositoryTests
    {
        private readonly Fixture _fixture;
        private readonly Mock<IDateTimeService> _dateTimeService;
        private readonly Mock<IAuthenticatedUserService> _authenticatedUserService;
        private readonly ApplicationDbContext _context;

        public TimeTrackerRepositoryTests()
        {
            _fixture = new Fixture();
            _dateTimeService = new Mock<IDateTimeService>();
            _authenticatedUserService = new Mock<IAuthenticatedUserService>();

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(_fixture.Create<string>());

            _context = new ApplicationDbContext(optionsBuilder.Options, _dateTimeService.Object, _authenticatedUserService.Object);
        }

        [Fact]
        public async Task HasOverlappingEntryAsync_WhenIntervalsOverlap_ShouldReturnTrue()
        {
            var userId = "user-1";
            var start = DateTime.UtcNow.Date.AddHours(9);

            _context.TimeEntries.Add(new TimeEntry
            {
                UserId = userId,
                ProjectId = 1,
                EntryDate = start.Date,
                StartTimeUtc = start,
                EndTimeUtc = start.AddHours(1),
                DurationMinutes = 60,
                SourceType = "Manual"
            });
            await _context.SaveChangesAsync();

            var repository = new TimeEntryRepositoryAsync(_context);
            var result = await repository.HasOverlappingEntryAsync(userId, start.AddMinutes(15), start.AddMinutes(45));

            Assert.True(result);
        }

        [Fact]
        public async Task HasOverlappingEntryAsync_WhenIntervalsDoNotOverlap_ShouldReturnFalse()
        {
            var userId = "user-2";
            var start = DateTime.UtcNow.Date.AddHours(9);

            _context.TimeEntries.Add(new TimeEntry
            {
                UserId = userId,
                ProjectId = 2,
                EntryDate = start.Date,
                StartTimeUtc = start,
                EndTimeUtc = start.AddHours(1),
                DurationMinutes = 60,
                SourceType = "Manual"
            });
            await _context.SaveChangesAsync();

            var repository = new TimeEntryRepositoryAsync(_context);
            var result = await repository.HasOverlappingEntryAsync(userId, start.AddHours(1), start.AddHours(2));

            Assert.False(result);
        }

        [Fact]
        public async Task GetActiveByUserIdAsync_WhenTimerExists_ShouldReturnTimer()
        {
            var userId = "user-3";
            _context.RunningTimers.Add(new RunningTimer
            {
                UserId = userId,
                ProjectId = 3,
                StartedAtUtc = DateTime.UtcNow.AddMinutes(-10)
            });
            await _context.SaveChangesAsync();

            var repository = new RunningTimerRepositoryAsync(_context);
            var timer = await repository.GetActiveByUserIdAsync(userId);

            Assert.NotNull(timer);
            Assert.Equal(userId, timer.UserId);
        }

        [Fact]
        public async Task IsUserAssignedToProjectAsync_WhenAssignmentActive_ShouldReturnTrue()
        {
            const string userId = "user-4";
            const int projectId = 5;

            _context.ProjectAssignments.Add(new ProjectAssignment
            {
                UserId = userId,
                ProjectId = projectId,
                AssignedAtUtc = DateTime.UtcNow,
                IsActive = true
            });
            await _context.SaveChangesAsync();

            var repository = new ProjectAssignmentRepositoryAsync(_context);
            var assigned = await repository.IsUserAssignedToProjectAsync(userId, projectId);

            Assert.True(assigned);
        }

        [Fact]
        public async Task GetPagedByManagedProjectsAsync_WhenManagerScopesApplied_ShouldReturnOnlyManagedEmployeeEntries()
        {
            const string managerUserId = "manager-1";
            const string anotherManagerUserId = "manager-2";
            const string employeeUserId = "employee-1";

            _context.Projects.AddRange(
                new Project { Id = 100, Name = "P1", ManagerUserId = managerUserId, Status = "Active" },
                new Project { Id = 101, Name = "P2", ManagerUserId = anotherManagerUserId, Status = "Active" });

            _context.TimeEntries.AddRange(
                new TimeEntry
                {
                    UserId = employeeUserId,
                    ProjectId = 100,
                    EntryDate = DateTime.UtcNow.Date,
                    DurationMinutes = 30,
                    SourceType = "Manual",
                    Description = "managed-project-entry"
                },
                new TimeEntry
                {
                    UserId = managerUserId,
                    ProjectId = 100,
                    EntryDate = DateTime.UtcNow.Date,
                    DurationMinutes = 10,
                    SourceType = "Manual",
                    Description = "manager-own-entry"
                },
                new TimeEntry
                {
                    UserId = employeeUserId,
                    ProjectId = 101,
                    EntryDate = DateTime.UtcNow.Date,
                    DurationMinutes = 45,
                    SourceType = "Manual",
                    Description = "other-manager-project-entry"
                });

            await _context.SaveChangesAsync();

            var repository = new TimeEntryRepositoryAsync(_context);
            var result = await repository.GetPagedByManagedProjectsAsync(
                managerUserId,
                pageNumber: 1,
                pageSize: 20,
                employeeUserId: employeeUserId);

            Assert.Single(result);
            Assert.Equal("managed-project-entry", result[0].Description);
            Assert.Equal(100, result[0].ProjectId);
            Assert.Equal(employeeUserId, result[0].UserId);
        }

        [Fact]
        public async Task GetProjectSummaryByManagedProjectsAsync_WhenManagerScopesApplied_ShouldAggregateByProject()
        {
            const string managerUserId = "manager-10";
            const string employeeA = "employee-a";
            const string employeeB = "employee-b";

            _context.Projects.AddRange(
                new Project { Id = 200, Name = "P-Managed", ManagerUserId = managerUserId, Status = "Active" },
                new Project { Id = 201, Name = "P-Other", ManagerUserId = "other-manager", Status = "Active" });

            _context.TimeEntries.AddRange(
                new TimeEntry { UserId = employeeA, ProjectId = 200, EntryDate = DateTime.UtcNow.Date, DurationMinutes = 30, SourceType = "Manual" },
                new TimeEntry { UserId = employeeB, ProjectId = 200, EntryDate = DateTime.UtcNow.Date, DurationMinutes = 90, SourceType = "Manual" },
                new TimeEntry { UserId = managerUserId, ProjectId = 200, EntryDate = DateTime.UtcNow.Date, DurationMinutes = 120, SourceType = "Manual" },
                new TimeEntry { UserId = employeeA, ProjectId = 201, EntryDate = DateTime.UtcNow.Date, DurationMinutes = 45, SourceType = "Manual" });

            await _context.SaveChangesAsync();

            var repository = new TimeEntryRepositoryAsync(_context);
            var summary = await repository.GetProjectSummaryByManagedProjectsAsync(managerUserId);

            Assert.Single(summary);
            Assert.Equal(200, summary[0].ProjectId);
            Assert.Equal(120, summary[0].TotalDurationMinutes);
            Assert.Equal(2, summary[0].EntryCount);
            Assert.Equal(2, summary[0].EmployeeCount);
        }

        [Fact]
        public async Task GetPeriodSummaryByManagedProjectsAsync_WhenFiltersApplied_ShouldAggregateByDate()
        {
            const string managerUserId = "manager-20";
            var day1 = DateTime.UtcNow.Date.AddDays(-1);
            var day2 = DateTime.UtcNow.Date;

            _context.Projects.Add(new Project { Id = 300, Name = "P-Managed", ManagerUserId = managerUserId, Status = "Active" });

            _context.TimeEntries.AddRange(
                new TimeEntry { UserId = "employee-a", ProjectId = 300, EntryDate = day1, DurationMinutes = 20, SourceType = "Manual" },
                new TimeEntry { UserId = "employee-a", ProjectId = 300, EntryDate = day1, DurationMinutes = 40, SourceType = "Manual" },
                new TimeEntry { UserId = "employee-b", ProjectId = 300, EntryDate = day2, DurationMinutes = 60, SourceType = "Manual" });

            await _context.SaveChangesAsync();

            var repository = new TimeEntryRepositoryAsync(_context);
            var summary = await repository.GetPeriodSummaryByManagedProjectsAsync(
                managerUserId,
                from: day1,
                to: day2,
                projectId: 300,
                employeeUserId: "employee-a");

            Assert.Single(summary);
            Assert.Equal(day1, summary[0].EntryDate);
            Assert.Equal(60, summary[0].TotalDurationMinutes);
            Assert.Equal(2, summary[0].EntryCount);
            Assert.Equal(1, summary[0].ProjectCount);
            Assert.Equal(1, summary[0].EmployeeCount);
        }

        [Fact]
        public async Task GetPagedByManagedProjectsAsync_WhenBillableAndSortApplied_ShouldReturnFilteredSortedRows()
        {
            const string managerUserId = "manager-30";

            _context.Projects.Add(new Project { Id = 400, Name = "P-Managed", ManagerUserId = managerUserId, Status = "Active" });
            _context.TimeEntries.AddRange(
                new TimeEntry
                {
                    UserId = "employee-a",
                    ProjectId = 400,
                    EntryDate = DateTime.UtcNow.Date,
                    DurationMinutes = 40,
                    IsBillable = true,
                    SourceType = "Manual"
                },
                new TimeEntry
                {
                    UserId = "employee-b",
                    ProjectId = 400,
                    EntryDate = DateTime.UtcNow.Date,
                    DurationMinutes = 20,
                    IsBillable = true,
                    SourceType = "Manual"
                },
                new TimeEntry
                {
                    UserId = "employee-c",
                    ProjectId = 400,
                    EntryDate = DateTime.UtcNow.Date,
                    DurationMinutes = 10,
                    IsBillable = false,
                    SourceType = "Manual"
                });
            await _context.SaveChangesAsync();

            var repository = new TimeEntryRepositoryAsync(_context);

            var result = await repository.GetPagedByManagedProjectsAsync(
                managerUserId,
                pageNumber: 1,
                pageSize: 10,
                projectId: 400,
                isBillable: true,
                sortBy: "durationMinutes",
                sortDir: "asc");

            Assert.Equal(2, result.Count);
            Assert.Equal(20, result[0].DurationMinutes);
            Assert.Equal(40, result[1].DurationMinutes);
            Assert.All(result, r => Assert.True(r.IsBillable));
        }

        [Fact]
        public async Task GetSummaryRowsByUserAsync_WhenAssignmentInactive_ShouldStillReturnHistoricalRows()
        {
            const string userId = "employee-h1";
            const int projectId = 500;

            _context.Projects.Add(new Project { Id = projectId, Name = "P-Historical", ManagerUserId = "manager-h1", Status = "Active" });
            _context.ProjectAssignments.Add(new ProjectAssignment
            {
                UserId = userId,
                ProjectId = projectId,
                AssignedAtUtc = DateTime.UtcNow.AddDays(-30),
                IsActive = false,
                UnassignedAtUtc = DateTime.UtcNow.AddDays(-1)
            });
            _context.TimeEntries.Add(new TimeEntry
            {
                UserId = userId,
                ProjectId = projectId,
                EntryDate = DateTime.UtcNow.Date.AddDays(-7),
                DurationMinutes = 75,
                SourceType = "Manual"
            });

            await _context.SaveChangesAsync();

            var repository = new TimeEntryRepositoryAsync(_context);
            var rows = await repository.GetSummaryRowsByUserAsync(userId);

            Assert.Single(rows);
            Assert.Equal(projectId, rows[0].ProjectId);
            Assert.Equal(75, rows[0].DurationMinutes);
        }

        [Fact]
        public async Task GetSummaryRowsByManagedProjectsAsync_WhenEmployeeUnassigned_ShouldStillReturnHistoricalRows()
        {
            const string managerUserId = "manager-h2";
            const string employeeUserId = "employee-h2";
            const int projectId = 501;

            _context.Projects.Add(new Project { Id = projectId, Name = "P-Managed-Historical", ManagerUserId = managerUserId, Status = "Active" });
            _context.ProjectAssignments.Add(new ProjectAssignment
            {
                UserId = employeeUserId,
                ProjectId = projectId,
                AssignedAtUtc = DateTime.UtcNow.AddDays(-20),
                IsActive = false,
                UnassignedAtUtc = DateTime.UtcNow.AddDays(-3)
            });
            _context.TimeEntries.Add(new TimeEntry
            {
                UserId = employeeUserId,
                ProjectId = projectId,
                EntryDate = DateTime.UtcNow.Date.AddDays(-10),
                DurationMinutes = 95,
                SourceType = "Manual"
            });

            await _context.SaveChangesAsync();

            var repository = new TimeEntryRepositoryAsync(_context);
            var rows = await repository.GetSummaryRowsByManagedProjectsAsync(
                managerUserId,
                projectId: projectId,
                employeeUserId: employeeUserId);

            Assert.Single(rows);
            Assert.Equal(employeeUserId, rows[0].UserId);
            Assert.Equal(projectId, rows[0].ProjectId);
            Assert.Equal(95, rows[0].DurationMinutes);
        }

        [Fact]
        public async Task GetPagedByManagerVisibilityAsync_ShouldIncludeManagerOwnAndManagedTeamExpenses()
        {
            const string managerUserId = "manager-exp-1";
            _context.Projects.AddRange(
                new Project { Id = 800, Name = "ManagedExpenseProject", ManagerUserId = managerUserId, Status = "Active" },
                new Project { Id = 801, Name = "OtherExpenseProject", ManagerUserId = "another-manager", Status = "Active" });
            _context.ExpenseCategories.Add(new ExpenseCategory { Id = 1, Name = "Travel", IsActive = true });
            _context.Expenses.AddRange(
                new Expense
                {
                    UserId = managerUserId,
                    ProjectId = 801,
                    CategoryId = 1,
                    ExpenseDate = DateTime.UtcNow.Date,
                    Amount = 10,
                    CurrencyCode = "TRY",
                    Status = "Draft"
                },
                new Expense
                {
                    UserId = "employee-exp-1",
                    ProjectId = 800,
                    CategoryId = 1,
                    ExpenseDate = DateTime.UtcNow.Date,
                    Amount = 20,
                    CurrencyCode = "TRY",
                    Status = "Submitted"
                },
                new Expense
                {
                    UserId = "employee-exp-2",
                    ProjectId = 801,
                    CategoryId = 1,
                    ExpenseDate = DateTime.UtcNow.Date,
                    Amount = 30,
                    CurrencyCode = "TRY",
                    Status = "Submitted"
                });
            await _context.SaveChangesAsync();

            var repository = new ExpenseRepositoryAsync(_context);
            var rows = await repository.GetPagedByManagerVisibilityAsync(managerUserId, 1, 20);

            Assert.Equal(2, rows.Count);
            Assert.Contains(rows, e => e.UserId == managerUserId);
            Assert.Contains(rows, e => e.UserId == "employee-exp-1");
            Assert.DoesNotContain(rows, e => e.UserId == "employee-exp-2");
        }
    }
}
