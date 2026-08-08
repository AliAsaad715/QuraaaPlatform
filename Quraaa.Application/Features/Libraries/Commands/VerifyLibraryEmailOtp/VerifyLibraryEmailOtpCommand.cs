using MediatR;
using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Libraries.Commands.VerifyLibraryEmailOtp
{
    public sealed record VerifyLibraryEmailOtpCommand(
        string Token,
        Guid VerificationId,
        string OtpCode)
        : IRequest<AppResult<LibraryEmailVerificationResponse>>;
}
