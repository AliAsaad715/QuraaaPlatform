using MediatR;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Otp.Commands.SendOtp
{
    public record SendOtpCommand(
        string PhoneNumber,
        string SmsGatewayDeviceToken,
        string? ClientIpAddress = null
    ) : IRequest<AppResult>;
}
