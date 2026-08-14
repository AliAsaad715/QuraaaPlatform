using MediatR;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Categories.Commands.UpdateCategory
{
    public record UpdateCategoryCommand(
        Guid Id,
        string NameAr,
        string NameEn,
        Guid ModifiedBy
    ) : IRequest<AppResult>;
}
