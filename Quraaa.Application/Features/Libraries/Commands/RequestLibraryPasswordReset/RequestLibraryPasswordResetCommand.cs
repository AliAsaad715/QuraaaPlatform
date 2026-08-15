using MediatR;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Libraries.Commands.RequestLibraryPasswordReset
{
    /// <summary>
    /// Anonymous command: emails a one-time code to a library so its owner can
    /// set a new dashboard password. Always reports success, so it cannot be
    /// used to discover which library emails exist.
    /// </summary>
    public record RequestLibraryPasswordResetCommand(string Email) : IRequest<AppResult>;
}
