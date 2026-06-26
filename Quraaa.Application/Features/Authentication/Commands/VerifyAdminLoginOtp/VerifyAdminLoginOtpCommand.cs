using MediatR;
using Quraaa.Application.Features.Authentication.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Authentication.Commands.VerifyAdminLoginOtp
{
    public record VerifyAdminLoginOtpCommand(
        string PhoneNumber,
        string OtpCode,
        string ClientIp
    ) : IRequest<AppResult<AuthResponse>>;
}
