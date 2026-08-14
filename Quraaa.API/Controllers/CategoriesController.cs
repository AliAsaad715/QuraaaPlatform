using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.Application.Features.Categories.Commands.CreateCategory;
using Quraaa.Application.Features.Categories.Commands.DeleteCategory;
using Quraaa.Application.Features.Categories.Commands.UpdateCategory;
using Quraaa.Application.Features.Categories.Common;
using Quraaa.Application.Features.Categories.Queries.GetAllCategories;
using Quraaa.Application.Features.Categories.Queries.GetCategoryById;

namespace Quraaa.API.Controllers
{
    public class CategoriesController : ApiClientController
    {
        [HttpGet]
        [ProducesResponseType(typeof(List<CategoryResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllCategories()
        {
            var result = await Mediator.Send(new GetAllCategoriesQuery());
            return HandleResult(result);
        }

        [HttpGet("{categoryId}")]
        [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCategoryById([FromRoute] Guid categoryId)
        {
            var result = await Mediator.Send(new GetCategoryByIdQuery(categoryId));
            return HandleResult(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryCommand request)
        {
            var result = await Mediator.Send(request);
            return HandleResult(result);
        }

        /// <summary>
        /// Updates a category's Arabic/English names. The category code and parent
        /// are immutable after creation.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{categoryId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCategory(
            [FromRoute] Guid categoryId,
            [FromBody] UpdateCategoryCommand command,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var adminId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                command with { Id = categoryId, ModifiedBy = adminId },
                cancellationToken);

            return HandleResult(result);
        }

        /// <summary>
        /// Deletes a category. Fails with a conflict if any book still references it.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{categoryId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteCategory(
            [FromRoute] Guid categoryId,
            CancellationToken cancellationToken = default)
        {
            var result = await Mediator.Send(new DeleteCategoryCommand(categoryId), cancellationToken);
            return HandleResult(result);
        }
    }
}
