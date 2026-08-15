using MediatR;
using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Files;

namespace Quraaa.Application.Features.Libraries.Commands.RegisterLibrary
{
    /// <param name="Password">
    /// The password for the library dashboard. It is the library's own
    /// credential and must differ from the owner's personal account password.
    /// </param>
    /// <param name="ConfirmPassword">Must repeat <paramref name="Password"/>.</param>
    public record RegisterLibraryCommand(
        string Token,
        string LibraryName,
        string Location,
        IUploadedFile? LibraryImage,
        IUploadedFile? HeaderImage,
        string Email,
        string Password,
        string ConfirmPassword
    ) : IRequest<AppResult<LibraryRegistrationSubmissionResponse>>;
}
