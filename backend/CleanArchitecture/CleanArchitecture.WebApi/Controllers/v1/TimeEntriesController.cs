using CleanArchitecture.Core.Features.TimeEntries.Commands.CreateTimeEntry;
using CleanArchitecture.Core.Features.TimeEntries.Commands.DeleteTimeEntryById;
using CleanArchitecture.Core.Features.TimeEntries.Commands.UpdateTimeEntry;
using CleanArchitecture.Core.Features.TimeEntries.Queries.GetAllTimeEntries;
using CleanArchitecture.Core.Features.TimeEntries.Queries.GetTeamPeriodSummary;
using CleanArchitecture.Core.Features.TimeEntries.Queries.GetTeamProjectSummary;
using CleanArchitecture.Core.Features.TimeEntries.Queries.GetTeamTimeEntries;
using CleanArchitecture.Core.Features.TimeEntries.Queries.GetTimeEntryById;
using CleanArchitecture.Core.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CleanArchitecture.WebApi.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize]
    public class TimeEntriesController : BaseApiController
    {
        [HttpGet]
        [Authorize(Policy = "TimeEntries.Manage.Self")]
        public async Task<PagedResponse<GetAllTimeEntriesViewModel>> Get([FromQuery] GetAllTimeEntriesParameter filter)
        {
            return await Mediator.Send(new GetAllTimeEntriesQuery
            {
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                ProjectId = filter.ProjectId,
                From = filter.From,
                To = filter.To,
                IsBillable = filter.IsBillable,
                SortBy = filter.SortBy,
                SortDir = filter.SortDir
            });
        }

        [HttpGet("team")]
        [Authorize(Policy = "TimeEntries.Read.Team")]
        public async Task<PagedResponse<GetAllTimeEntriesViewModel>> GetTeam([FromQuery] GetTeamTimeEntriesParameter filter)
        {
            return await Mediator.Send(new GetTeamTimeEntriesQuery
            {
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                ProjectId = filter.ProjectId,
                EmployeeUserId = filter.EmployeeUserId,
                From = filter.From,
                To = filter.To,
                IsBillable = filter.IsBillable,
                SortBy = filter.SortBy,
                SortDir = filter.SortDir
            });
        }

        [HttpGet("team/summary/projects")]
        [Authorize(Policy = "TimeEntries.Read.Team")]
        public async Task<List<GetTeamProjectSummaryViewModel>> GetTeamProjectSummary([FromQuery] GetTeamProjectSummaryParameter filter)
        {
            return await Mediator.Send(new GetTeamProjectSummaryQuery
            {
                From = filter.From,
                To = filter.To,
                EmployeeUserId = filter.EmployeeUserId
            });
        }

        [HttpGet("team/summary/period")]
        [Authorize(Policy = "TimeEntries.Read.Team")]
        public async Task<List<GetTeamPeriodSummaryViewModel>> GetTeamPeriodSummary([FromQuery] GetTeamPeriodSummaryParameter filter)
        {
            return await Mediator.Send(new GetTeamPeriodSummaryQuery
            {
                From = filter.From,
                To = filter.To,
                ProjectId = filter.ProjectId,
                EmployeeUserId = filter.EmployeeUserId
            });
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = "TimeEntries.Manage.Self")]
        public async Task<IActionResult> Get(int id)
        {
            return Ok(await Mediator.Send(new GetTimeEntryByIdQuery { Id = id }));
        }

        [HttpPost]
        [Authorize(Policy = "TimeEntries.Manage.Self")]
        public async Task<IActionResult> Post(CreateTimeEntryCommand command)
        {
            return Ok(await Mediator.Send(command));
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "TimeEntries.Manage.Self")]
        public async Task<IActionResult> Put(int id, UpdateTimeEntryCommand command)
        {
            if (id != command.Id)
            {
                return BadRequest();
            }

            return Ok(await Mediator.Send(command));
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "TimeEntries.Manage.Self")]
        public async Task<IActionResult> Delete(int id)
        {
            return Ok(await Mediator.Send(new DeleteTimeEntryByIdCommand { Id = id }));
        }
    }
}
