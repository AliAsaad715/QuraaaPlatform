using MediatR;
using Quraaa.Application.Features.Categories.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Categories.Queries.GetCategoryById
{
    public record GetCategoryByIdQuery(Guid CategoryId) : IRequest<AppResult<CategoryResponse>>;
}