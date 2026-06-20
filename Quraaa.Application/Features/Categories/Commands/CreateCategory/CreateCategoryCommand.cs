using MediatR;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Categories.Commands.CreateCategory
{
    public record CreateCategoryCommand(
        string Code,
        string NameAr,
        string NameEn,
        Guid? ParentCategoryId = null
    ) : IRequest<AppResult>;
}