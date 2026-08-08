using MediatR;
using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Libraries.Commands.ResendLibraryEmailOtp
{
    public sealed record ResendLibraryEmailOtpCommand(string Token)
        : IRequest<AppResult<LibraryEmailOtpResponse>>;
}
