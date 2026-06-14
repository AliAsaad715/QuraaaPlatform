using MediatR;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Otp.Commands.VerifyOtp
{
    public record VerifyOtpCommand(
        string PhoneNumber,
        string Code,
        string? ClientIpAddress = null
    ) : IRequest<AppResult>;
}
