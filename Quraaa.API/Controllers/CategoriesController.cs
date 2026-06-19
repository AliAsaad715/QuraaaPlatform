using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.Application.Features.Categories.Commands.CreateCategory;
using Quraaa.Application.Features.Categories.Common;
using Quraaa.Application.Features.Categories.Queries.GetAllCategories;
using Quraaa.Application.Features.Categories.Queries.GetCategoryById;

namespace Quraaa.API.Controllers
{
    public class CategoriesController : ApiClientController
    {
        [HttpGet]
        [ProducesResponseType(typeof(List<CategoryResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllCategories()
        {
            var result = await Mediator.Send(new GetAllCategoriesQuery());
            return HandleResult(result);
        }

        [HttpGet("{categoryId}")]
        [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCategoryById([FromRoute] Guid categoryId)
        {
            var result = await Mediator.Send(new GetCategoryByIdQuery(categoryId));
            return HandleResult(result);
        }

        //[Authorize]
        //[HttpPost]
        //[ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status401Unauthorized)]
        //[ProducesResponseType(StatusCodes.Status403Forbidden)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
        //{
        //    var command = new CreateCategoryCommand(request.Name, request.Description);
        //    var result = await Mediator.Send(command);
        //    return HandleResult(result);
        //}
    }
}