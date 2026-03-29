using CleanArchitecture.Core.Features.Reports.Models;
using CleanArchitecture.Core.Features.Reports.Queries.GetManagerTeamTimeSummary;
using CleanArchitecture.Core.Features.Reports.Queries.GetMyTimeSummary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.WebApi.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize]
    [Route("api/v{version:apiVersion}/reports")]
    public class ReportsController : BaseApiController
    {
        [HttpGet("me/time-summary")]
        [Authorize(Policy = "Reports.Read.Self")]
        public async Task<ActionResult<TimeSummaryResponse>> GetMyTimeSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string groupBy)
        {
            var result = await Mediator.Send(new GetMyTimeSummaryQuery
            {
                From = from,
                To = to,
                GroupBy = groupBy
            });

            return Ok(result);
        }

        [HttpGet("manager/team-time-summary")]
        [Authorize(Policy = "Reports.Read.Team")]
        public async Task<ActionResult<TimeSummaryResponse>> GetManagerTeamTimeSummary(
            [FromQuery] int? projectId,
            [FromQuery] string userId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string groupBy)
        {
            var result = await Mediator.Send(new GetManagerTeamTimeSummaryQuery
            {
                ProjectId = projectId,
                UserId = userId,
                From = from,
                To = to,
                GroupBy = groupBy
            });

            return Ok(result);
        }

        [HttpGet("me/time-summary/export")]
        [Authorize(Policy = "Reports.Export.Self")]
        public async Task<IActionResult> ExportMyTimeSummary(
            [FromQuery] string format,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string groupBy)
        {
            if (!string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Only format=csv is supported.");
            }

            var summary = await Mediator.Send(new GetMyTimeSummaryQuery
            {
                From = from,
                To = to,
                GroupBy = groupBy
            });

            return BuildCsvFile(summary, "my-time-summary");
        }

        [HttpGet("manager/team-time-summary/export")]
        [Authorize(Policy = "Reports.Export.Team")]
        public async Task<IActionResult> ExportManagerTeamTimeSummary(
            [FromQuery] string format,
            [FromQuery] int? projectId,
            [FromQuery] string userId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string groupBy)
        {
            if (!string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Only format=csv is supported.");
            }

            var summary = await Mediator.Send(new GetManagerTeamTimeSummaryQuery
            {
                ProjectId = projectId,
                UserId = userId,
                From = from,
                To = to,
                GroupBy = groupBy
            });

            return BuildCsvFile(summary, "team-time-summary");
        }

        private static FileContentResult BuildCsvFile(TimeSummaryResponse summary, string prefix)
        {
            var sb = new StringBuilder();
            sb.AppendLine("key,minutes");

            foreach (var row in summary.Groups.OrderBy(g => g.Key))
            {
                var escapedKey = row.Key?.Replace("\"", "\"\"") ?? string.Empty;
                sb.Append('"').Append(escapedKey).Append('"').Append(',').Append(row.Minutes).AppendLine();
            }

            var fileName = $"{prefix}-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            return new FileContentResult(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv")
            {
                FileDownloadName = fileName
            };
        }
    }
}
