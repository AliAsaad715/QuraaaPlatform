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
    }
}