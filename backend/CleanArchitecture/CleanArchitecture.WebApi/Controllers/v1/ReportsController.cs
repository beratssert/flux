using CleanArchitecture.Core.Features.Reports.Models;
using CleanArchitecture.Core.Features.Reports.Queries.GetManagerTeamExpenseSummary;
using CleanArchitecture.Core.Features.Reports.Queries.GetManagerTeamTimeSummary;
using CleanArchitecture.Core.Features.Reports.Queries.GetMyExpenseSummary;
using CleanArchitecture.Core.Features.Reports.Queries.GetMyTimeSummary;
using CleanArchitecture.Core.Features.Reports.Queries.GetProjectSummary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.WebApi.Controllers.v1
{
    /// <summary>Read-only analytics from time entries and expenses (no report table). Historical rows are not tied to active project assignment.</summary>
    /// <remarks>Self endpoints: policy <c>Reports.Read.Self</c> / <c>Reports.Export.Self</c>. Team endpoints: <c>Reports.Read.Team</c> / <c>Reports.Export.Team</c> (Manager scoped; Admin uses the same team routes for org-wide filters).</remarks>
    [ApiVersion("1.0")]
    [Authorize]
    [Route("api/v{version:apiVersion}/reports")]
    public class ReportsController : BaseApiController
    {
        /// <summary>Current user&apos;s time totals grouped by day, week, month, or project.</summary>
        /// <remarks>Policy: <c>Reports.Read.Self</c>. Query <c>groupBy</c>: <c>day</c> (default), <c>week</c>, <c>month</c>, <c>project</c>. Invalid <c>groupBy</c> returns 400.</remarks>
        [HttpGet("me/time-summary")]
        [Authorize(Policy = "Reports.Read.Self")]
        [ProducesResponseType(typeof(TimeSummaryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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

        /// <summary>Team time summary: Manager sees managed projects only; Admin sees all (optional <c>projectId</c> / <c>userId</c> filters).</summary>
        /// <remarks>Policy: <c>Reports.Read.Team</c>. <c>groupBy</c>: <c>user</c> (default), <c>project</c>, <c>week</c>.</remarks>
        [HttpGet("manager/team-time-summary")]
        [Authorize(Policy = "Reports.Read.Team")]
        [ProducesResponseType(typeof(TimeSummaryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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

        /// <summary>Current user&apos;s expense totals grouped by category, project, or month.</summary>
        /// <remarks>Policy: <c>Reports.Read.Self</c>. <c>groupBy</c>: <c>category</c> (default), <c>project</c>, <c>month</c>. Optional <c>currencyCode</c> filter.</remarks>
        [HttpGet("me/expense-summary")]
        [Authorize(Policy = "Reports.Read.Self")]
        [ProducesResponseType(typeof(ExpenseSummaryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ExpenseSummaryResponse>> GetMyExpenseSummary(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string groupBy,
            [FromQuery] string currencyCode)
        {
            var result = await Mediator.Send(new GetMyExpenseSummaryQuery
            {
                From = from,
                To = to,
                GroupBy = groupBy,
                CurrencyCode = currencyCode
            });

            return Ok(result);
        }

        /// <summary>Team expense summary with optional <c>projectId</c>, <c>userId</c>, <c>categoryId</c>, and currency filter.</summary>
        /// <remarks>Policy: <c>Reports.Read.Team</c>. <c>groupBy</c>: <c>user</c> (default), <c>project</c>, <c>month</c>.</remarks>
        [HttpGet("manager/team-expense-summary")]
        [Authorize(Policy = "Reports.Read.Team")]
        [ProducesResponseType(typeof(ExpenseSummaryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ExpenseSummaryResponse>> GetManagerTeamExpenseSummary(
            [FromQuery] int? projectId,
            [FromQuery] string userId,
            [FromQuery] int? categoryId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string groupBy,
            [FromQuery] string currencyCode)
        {
            var result = await Mediator.Send(new GetManagerTeamExpenseSummaryQuery
            {
                ProjectId = projectId,
                UserId = userId,
                CategoryId = categoryId,
                From = from,
                To = to,
                GroupBy = groupBy,
                CurrencyCode = currencyCode
            });

            return Ok(result);
        }

        /// <summary>Aggregate for one project: total minutes, total expense amount, billable time-entry rate (%).</summary>
        /// <remarks>Policy: <c>Reports.Read.Team</c>. Optional <c>from</c>/<c>to</c> filter by time entry date and expense date (inclusive, date-only). Manager: project must be managed by caller. Unknown id or inaccessible project: <c>404</c>.</remarks>
        [HttpGet("projects/{projectId:int}/summary")]
        [Authorize(Policy = "Reports.Read.Team")]
        [ProducesResponseType(typeof(ProjectSummaryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProjectSummaryResponse>> GetProjectSummary(
            int projectId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var result = await Mediator.Send(new GetProjectSummaryQuery
            {
                ProjectId = projectId,
                From = from,
                To = to
            });

            return Ok(result);
        }

        /// <summary>Download project aggregate summary as CSV (<c>format=csv</c>). Optional <c>from</c>/<c>to</c> match the JSON endpoint.</summary>
        /// <remarks>Policy: <c>Reports.Export.Team</c>.</remarks>
        [HttpGet("projects/{projectId:int}/summary/export")]
        [Authorize(Policy = "Reports.Export.Team")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ExportProjectSummary(
            int projectId,
            [FromQuery] string format,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            if (!string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Only format=csv is supported.");
            }

            var summary = await Mediator.Send(new GetProjectSummaryQuery
            {
                ProjectId = projectId,
                From = from,
                To = to
            });

            return BuildProjectSummaryCsv(summary, from, to);
        }

        /// <summary>Download current user time summary as CSV (<c>format=csv</c> required).</summary>
        /// <remarks>Policy: <c>Reports.Export.Self</c>. Same <c>from</c>, <c>to</c>, <c>groupBy</c> as JSON endpoint.</remarks>
        [HttpGet("me/time-summary/export")]
        [Authorize(Policy = "Reports.Export.Self")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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

        /// <summary>Download team time summary as CSV.</summary>
        /// <remarks>Policy: <c>Reports.Export.Team</c>. Query mirrors <c>GET .../manager/team-time-summary</c>.</remarks>
        [HttpGet("manager/team-time-summary/export")]
        [Authorize(Policy = "Reports.Export.Team")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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

        /// <summary>Download current user expense summary as CSV.</summary>
        /// <remarks>Policy: <c>Reports.Export.Self</c>. Optional <c>currencyCode</c>.</remarks>
        [HttpGet("me/expense-summary/export")]
        [Authorize(Policy = "Reports.Export.Self")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ExportMyExpenseSummary(
            [FromQuery] string format,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string groupBy,
            [FromQuery] string currencyCode)
        {
            if (!string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Only format=csv is supported.");
            }

            var summary = await Mediator.Send(new GetMyExpenseSummaryQuery
            {
                From = from,
                To = to,
                GroupBy = groupBy,
                CurrencyCode = currencyCode
            });

            return BuildExpenseCsvFile(summary, "my-expense-summary");
        }

        /// <summary>Download team expense summary as CSV.</summary>
        /// <remarks>Policy: <c>Reports.Export.Team</c>. Query mirrors <c>GET .../manager/team-expense-summary</c>.</remarks>
        [HttpGet("manager/team-expense-summary/export")]
        [Authorize(Policy = "Reports.Export.Team")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ExportManagerTeamExpenseSummary(
            [FromQuery] string format,
            [FromQuery] int? projectId,
            [FromQuery] string userId,
            [FromQuery] int? categoryId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string groupBy,
            [FromQuery] string currencyCode)
        {
            if (!string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Only format=csv is supported.");
            }

            var summary = await Mediator.Send(new GetManagerTeamExpenseSummaryQuery
            {
                ProjectId = projectId,
                UserId = userId,
                CategoryId = categoryId,
                From = from,
                To = to,
                GroupBy = groupBy,
                CurrencyCode = currencyCode
            });

            return BuildExpenseCsvFile(summary, "team-expense-summary");
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

        private static FileContentResult BuildExpenseCsvFile(ExpenseSummaryResponse summary, string prefix)
        {
            var sb = new StringBuilder();
            sb.AppendLine("key,amount");

            foreach (var row in summary.Groups.OrderBy(g => g.Key))
            {
                var escapedKey = row.Key?.Replace("\"", "\"\"") ?? string.Empty;
                sb.Append('"').Append(escapedKey).Append('"').Append(',').Append(row.Amount).AppendLine();
            }

            var fileName = $"{prefix}-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            return new FileContentResult(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv")
            {
                FileDownloadName = fileName
            };
        }

        private static FileContentResult BuildProjectSummaryCsv(
            ProjectSummaryResponse summary,
            DateTime? from,
            DateTime? to)
        {
            var sb = new StringBuilder();
            sb.AppendLine("projectId,totalMinutes,totalExpenseAmount,billableEntryRate,from,to");
            sb.Append(summary.ProjectId).Append(',')
                .Append(summary.TotalMinutes).Append(',')
                .Append(summary.TotalExpenseAmount).Append(',')
                .Append(summary.BillableEntryRate).Append(',')
                .Append(from.HasValue ? from.Value.ToString("yyyy-MM-dd") : string.Empty).Append(',')
                .Append(to.HasValue ? to.Value.ToString("yyyy-MM-dd") : string.Empty)
                .AppendLine();

            var fileName = $"project-summary-{summary.ProjectId}-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            return new FileContentResult(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv")
            {
                FileDownloadName = fileName
            };
        }
    }
}
