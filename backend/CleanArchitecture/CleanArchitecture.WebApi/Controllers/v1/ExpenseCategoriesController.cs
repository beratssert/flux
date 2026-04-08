using CleanArchitecture.Core.Features.ExpenseCategories.Commands.CreateExpenseCategory;
using CleanArchitecture.Core.Features.ExpenseCategories.Commands.UpdateExpenseCategory;
using CleanArchitecture.Core.Features.ExpenseCategories.Queries.GetAllExpenseCategories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CleanArchitecture.WebApi.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize]
    [Route("api/v{version:apiVersion}/expense-categories")]
    public class ExpenseCategoriesController : BaseApiController
    {
        /// <summary>
        /// Lists active expense categories.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<GetAllExpenseCategoriesViewModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<List<GetAllExpenseCategoriesViewModel>> Get()
        {
            return await Mediator.Send(new GetAllExpenseCategoriesQuery());
        }

        /// <summary>
        /// Creates a new expense category.
        /// </summary>
        /// <remarks>
        /// Admin only.
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Post(CreateExpenseCategoryCommand command)
        {
            var id = await Mediator.Send(command);
            return StatusCode(StatusCodes.Status201Created, id);
        }

        /// <summary>
        /// Updates name and active flag of an expense category.
        /// </summary>
        /// <remarks>
        /// Admin only.
        /// </remarks>
        [HttpPatch("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Patch(int id, UpdateExpenseCategoryCommand command)
        {
            if (id != command.Id)
            {
                return BadRequest();
            }

            return Ok(await Mediator.Send(command));
        }
    }
}
