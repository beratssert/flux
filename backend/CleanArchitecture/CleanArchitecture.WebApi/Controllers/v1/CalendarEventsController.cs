using CleanArchitecture.Core.Features.CalendarEvents;
using CleanArchitecture.Core.Features.CalendarEvents.Commands.CreateCalendarEvent;
using CleanArchitecture.Core.Features.CalendarEvents.Commands.DeleteCalendarEvent;
using CleanArchitecture.Core.Features.CalendarEvents.Commands.UpdateCalendarEvent;
using CleanArchitecture.Core.Features.CalendarEvents.Queries.GetCalendarEventById;
using CleanArchitecture.Core.Features.CalendarEvents.Queries.GetCalendarEvents;
using CleanArchitecture.Core.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace CleanArchitecture.WebApi.Controllers.v1
{
    /// <summary>Project-focused calendar events (MVP: managers create/update/delete; all roles read within scope).</summary>
    /// <remarks>
    /// Project identifiers are <strong>integers</strong>, consistent with <c>/projects</c>.
    /// Policies: <c>Calendar.Read.Self</c> for reads; <c>Calendar.Manage.OwnProject</c> for writes (Manager only).
    /// </remarks>
    [ApiVersion("1.0")]
    [Authorize]
    [Route("api/v{version:apiVersion}/calendar-events")]
    public class CalendarEventsController : BaseApiController
    {
        /// <summary>List events visible to the current user, with optional date range and filters.</summary>
        [HttpGet]
        [Authorize(Policy = "Calendar.Read.Self")]
        [ProducesResponseType(typeof(PagedResponse<CalendarEventViewModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<PagedResponse<CalendarEventViewModel>> Get(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int? projectId,
            [FromQuery] string visibilityType,
            [FromQuery] string userId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            return await Mediator.Send(new GetCalendarEventsQuery
            {
                From = from,
                To = to,
                ProjectId = projectId,
                VisibilityType = visibilityType,
                UserId = userId,
                PageNumber = page,
                PageSize = pageSize
            });
        }

        /// <summary>Get one event if it is visible to the caller.</summary>
        [HttpGet("{id:guid}")]
        [Authorize(Policy = "Calendar.Read.Self")]
        [ProducesResponseType(typeof(CalendarEventViewModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            return Ok(await Mediator.Send(new GetCalendarEventByIdQuery { Id = id }));
        }

        /// <summary>Create an event (Manager only; managed projects or personal with team participants).</summary>
        [HttpPost]
        [Authorize(Policy = "Calendar.Manage.OwnProject")]
        [ProducesResponseType(typeof(CalendarEventViewModel), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Post([FromBody] CreateCalendarEventCommand command)
        {
            var vm = await Mediator.Send(command);
            return CreatedAtAction(nameof(GetById), new { id = vm.Id }, vm);
        }

        /// <summary>Update an event the manager is allowed to change.</summary>
        [HttpPatch("{id:guid}")]
        [Authorize(Policy = "Calendar.Manage.OwnProject")]
        [ProducesResponseType(typeof(CalendarEventViewModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Patch(Guid id, [FromBody] UpdateCalendarEventCommand body)
        {
            body.Id = id;
            return Ok(await Mediator.Send(body));
        }

        /// <summary>Delete an event the manager is allowed to remove.</summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "Calendar.Manage.OwnProject")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            await Mediator.Send(new DeleteCalendarEventCommand { Id = id });
            return NoContent();
        }
    }
}
