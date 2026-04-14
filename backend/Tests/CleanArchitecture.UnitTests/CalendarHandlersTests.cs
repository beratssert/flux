using AutoMapper;
using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Features.CalendarEvents.Queries.GetCalendarEvents;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using CleanArchitecture.Core.Mappings;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CleanArchitecture.UnitTests;

public class CalendarHandlersTests
{
    private readonly IMapper _mapper = new MapperConfiguration(c => c.AddProfile<GeneralProfile>()).CreateMapper();

    [Fact]
    public async Task GetCalendarEvents_WhenEmployeeFiltersAnotherUser_ThrowsApiException()
    {
        var auth = new Mock<IAuthenticatedUserService>();
        auth.Setup(a => a.UserId).Returns("emp-1");
        auth.Setup(a => a.Role).Returns("Employee");

        var repo = new Mock<ICalendarEventRepositoryAsync>();
        var handler = new GetCalendarEventsQueryHandler(repo.Object, auth.Object, _mapper);

        await Assert.ThrowsAsync<ApiException>(() => handler.Handle(new GetCalendarEventsQuery
        {
            UserId = "other-user",
            PageNumber = 1,
            PageSize = 20
        }, CancellationToken.None));
    }

    [Fact]
    public async Task GetCalendarEvents_WhenEmployeeFiltersSelf_CallsRepository()
    {
        var auth = new Mock<IAuthenticatedUserService>();
        auth.Setup(a => a.UserId).Returns("emp-1");
        auth.Setup(a => a.Role).Returns("Employee");

        var repo = new Mock<ICalendarEventRepositoryAsync>();
        repo.Setup(r => r.GetPagedVisibleAsync(It.IsAny<CalendarEventListCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<CalendarEvent>(), 0));

        var handler = new GetCalendarEventsQueryHandler(repo.Object, auth.Object, _mapper);

        await handler.Handle(new GetCalendarEventsQuery
        {
            UserId = "emp-1",
            PageNumber = 1,
            PageSize = 20
        }, CancellationToken.None);

        repo.Verify(r => r.GetPagedVisibleAsync(
            It.Is<CalendarEventListCriteria>(c => c.FilterUserId == "emp-1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
