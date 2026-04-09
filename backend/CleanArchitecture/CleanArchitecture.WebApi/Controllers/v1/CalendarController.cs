using CleanArchitecture.Core.Features.Calendar.Commands.CreateCalendarEvent;
using CleanArchitecture.Core.Features.Calendar.Commands.DeleteCalendarEvent;
using CleanArchitecture.Core.Features.Calendar.Commands.UpdateCalendarEvent;
using CleanArchitecture.Core.Features.Calendar.Queries.GetCalendarItems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CleanArchitecture.WebApi.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize]
    public class CalendarController : BaseApiController
    {
        /// <summary>
        /// Returns calendar events and (optionally) time entries for the authenticated user
        /// within the given date range.
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "Calendar.Read.Self")]
        [ProducesResponseType(typeof(List<CalendarItemViewModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Get(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            [FromQuery] int? projectId = null,
            [FromQuery] bool includeTimeEntries = true)
        {
            if (to < from)
            {
                return BadRequest("'to' must be greater than or equal to 'from'.");
            }

            var result = await Mediator.Send(new GetCalendarItemsQuery
            {
                From = from,
                To = to,
                ProjectId = projectId,
                IncludeTimeEntries = includeTimeEntries,
            });

            return Ok(result);
        }

        /// <summary>Creates a new calendar event. Only Managers and Admins can create events.</summary>
        [HttpPost]
        [Authorize(Policy = "Calendar.Manage.OwnProject")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Post(CreateCalendarEventCommand command)
        {
            return Ok(await Mediator.Send(command));
        }

        /// <summary>Updates an existing calendar event. Only the creator or project manager can update.</summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "Calendar.Manage.OwnProject")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Put(int id, UpdateCalendarEventCommand command)
        {
            if (id != command.Id)
            {
                return BadRequest();
            }

            return Ok(await Mediator.Send(command));
        }

        /// <summary>Soft-deletes a calendar event.</summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "Calendar.Manage.OwnProject")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            return Ok(await Mediator.Send(new DeleteCalendarEventCommand { Id = id }));
        }
    }
}
