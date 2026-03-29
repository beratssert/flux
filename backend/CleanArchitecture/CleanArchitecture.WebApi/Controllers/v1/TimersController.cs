using CleanArchitecture.Core.Features.Timers.Commands.StartTimer;
using CleanArchitecture.Core.Features.Timers.Commands.StopTimer;
using CleanArchitecture.Core.Features.Timers.Queries.GetActiveTimer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CleanArchitecture.WebApi.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize(Policy = "RunningTimers.Manage.Self")]
    public class TimersController : BaseApiController
    {
        [HttpGet("active")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetActive()
        {
            return Ok(await Mediator.Send(new GetActiveTimerQuery()));
        }

        [HttpPost("start")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Start(StartTimerCommand command)
        {
            return Ok(await Mediator.Send(command));
        }

        [HttpPost("stop")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Stop()
        {
            return Ok(await Mediator.Send(new StopTimerCommand()));
        }
    }
}
