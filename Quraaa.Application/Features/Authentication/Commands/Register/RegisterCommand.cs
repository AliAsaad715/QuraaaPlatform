using MediatR;
using Quraaa.Application.Shared.Results;
using Quraaa.Domain.User.Enums;

namespace Quraaa.Application.Features.Authentication.Commands.Register
{
    public record RegisterCommand(
        string FirstName,
        string LastName,
        string PhoneNumber,
        string Password,
        Gender Gender,
        DateOnly DateOfBirth,
        List<Guid> Interests,
        string ClientIp
    ) : IRequest<AppResult>;
}
