using MediatR;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Categories.Commands.DeleteCategory
{
    public record DeleteCategoryCommand(
        Guid Id
    ) : IRequest<AppResult>;
}
