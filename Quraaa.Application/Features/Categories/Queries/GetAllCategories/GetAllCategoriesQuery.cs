using MediatR;
using Quraaa.Application.Features.Categories.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Categories.Queries.GetAllCategories
{
    public record GetAllCategoriesQuery : IRequest<AppResult<List<CategoryResponse>>>;
}