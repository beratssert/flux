using CleanArchitecture.Core.Features.Expenses.Commands.CreateExpense;
using CleanArchitecture.Core.Features.Expenses.Commands.DeleteExpenseById;
using CleanArchitecture.Core.Features.Expenses.Commands.RejectExpense;
using CleanArchitecture.Core.Features.Expenses.Commands.SubmitExpense;
using CleanArchitecture.Core.Features.Expenses.Commands.UpdateExpense;
using CleanArchitecture.Core.Features.Expenses.Queries.GetAllExpenses;
using CleanArchitecture.Core.Features.Expenses.Queries.GetExpenseById;
using CleanArchitecture.Core.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CleanArchitecture.WebApi.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize]
    public class ExpensesController : BaseApiController
    {
        /// <summary>
        /// Lists expenses with role-based visibility and filtering.
        /// </summary>
        /// <remarks>
        /// Employee sees own expenses, manager sees own plus managed-project team expenses, and admin sees all expenses.
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = "Employee,Manager,Admin")]
        [ProducesResponseType(typeof(PagedResponse<GetAllExpensesViewModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<PagedResponse<GetAllExpensesViewModel>> Get([FromQuery] GetAllExpensesParameter filter)
        {
            return await Mediator.Send(new GetAllExpensesQuery
            {
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                UserId = filter.UserId,
                ProjectId = filter.ProjectId,
                CategoryId = filter.CategoryId,
                Status = filter.Status,
                From = filter.From,
                To = filter.To,
                SortBy = filter.SortBy,
                SortDir = filter.SortDir
            });
        }

        /// <summary>
        /// Gets a single expense by id.
        /// </summary>
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Employee,Manager,Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(int id)
        {
            return Ok(await Mediator.Send(new GetExpenseByIdQuery { Id = id }));
        }

        /// <summary>
        /// Creates a new expense in Draft status.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "Expenses.Manage.Self")]
        [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Post(CreateExpenseCommand command)
        {
            var id = await Mediator.Send(command);
            return CreatedAtAction(nameof(Get), new { id }, id);
        }

        /// <summary>
        /// Updates an expense owned by the authenticated user.
        /// </summary>
        /// <remarks>
        /// Only Draft or Rejected expenses can be updated.
        /// </remarks>
        [HttpPatch("{id:int}")]
        [Authorize(Policy = "Expenses.Manage.Self")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Patch(int id, UpdateExpenseCommand command)
        {
            if (id != command.Id)
            {
                return BadRequest();
            }

            return Ok(await Mediator.Send(command));
        }

        /// <summary>
        /// Soft-deletes an expense owned by the authenticated user.
        /// </summary>
        /// <remarks>
        /// Only Draft expenses can be deleted.
        /// </remarks>
        [HttpDelete("{id:int}")]
        [Authorize(Policy = "Expenses.Manage.Self")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            await Mediator.Send(new DeleteExpenseByIdCommand { Id = id });
            return NoContent();
        }

        /// <summary>
        /// Submits a Draft or Rejected expense for review.
        /// </summary>
        [HttpPost("{id:int}/submit")]
        [Authorize(Policy = "Expenses.Manage.Self")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Submit(int id)
        {
            return Ok(await Mediator.Send(new SubmitExpenseCommand { Id = id }));
        }

        /// <summary>
        /// Rejects a Submitted expense in manager scope.
        /// </summary>
        [HttpPost("{id:int}/reject")]
        [Authorize(Policy = "Expenses.Reject.Team")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Reject(int id, RejectExpenseCommand command)
        {
            if (id != command.Id)
            {
                return BadRequest();
            }

            return Ok(await Mediator.Send(command));
        }
    }
}
