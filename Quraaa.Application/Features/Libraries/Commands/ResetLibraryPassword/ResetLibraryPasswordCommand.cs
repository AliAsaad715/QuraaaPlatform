using MediatR;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Libraries.Commands.ResetLibraryPassword
{
    /// <summary>
    /// Anonymous command: sets a new library dashboard password using the code
    /// emailed by the forgot-password endpoint.
    /// </summary>
    public record ResetLibraryPasswordCommand : IRequest<AppResult>
    {
        public required string Email { get; init; }

        /// <summary>The six-digit code from the email.</summary>
        public required string OtpCode { get; init; }

        public required string NewPassword { get; init; }

        public required string ConfirmNewPassword { get; init; }
    }
}
