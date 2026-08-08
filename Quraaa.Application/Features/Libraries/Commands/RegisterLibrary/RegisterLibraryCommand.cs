using MediatR;
using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Files;

namespace Quraaa.Application.Features.Libraries.Commands.RegisterLibrary
{
    public record RegisterLibraryCommand(
        string Token,
        string LibraryName,
        string Location,
        IUploadedFile? LibraryImage,
        IUploadedFile? HeaderImage,
        string Email
    ) : IRequest<AppResult<LibraryRegistrationSubmissionResponse>>;
}
