using MediatR;
using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Libraries.Commands.RegisterLibrary
{
    public record RegisterLibraryCommand(
        string LibraryName,
        string Location,
        string LibraryImage,
        string HeaderImage,
        string Email,
        Guid UserId
    ) : IRequest<AppResult<LibraryResponse>>;
}
