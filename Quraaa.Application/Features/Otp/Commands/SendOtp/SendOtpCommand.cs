using MediatR;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Otp.Commands.SendOtp
{
    public record SendOtpCommand(
        string PhoneNumber,
        string? ClientIpAddress = null
    ) : IRequest<AppResult>;
}
