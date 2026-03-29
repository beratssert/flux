using CleanArchitecture.Core.Features.Timers.Commands.StartTimer;
using CleanArchitecture.Core.Features.Timers.Commands.StopTimer;
using CleanArchitecture.Core.Features.Timers.Queries.GetActiveTimer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CleanArchitecture.WebApi.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize(Policy = "RunningTimers.Manage.Self")]
    public class TimersController : BaseApiController
    {
        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
        {
            return Ok(await Mediator.Send(new GetActiveTimerQuery()));
        }

        [HttpPost("start")]
        public async Task<IActionResult> Start(StartTimerCommand command)
        {
            return Ok(await Mediator.Send(command));
        }

        [HttpPost("stop")]
        public async Task<IActionResult> Stop()
        {
            return Ok(await Mediator.Send(new StopTimerCommand()));
        }
    }
}
