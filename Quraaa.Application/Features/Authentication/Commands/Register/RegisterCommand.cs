using MediatR;
using Quraaa.Application.Features.Authentication.Common;
using Quraaa.Application.Shared.Results;
using Quraaa.Domain.User.Enums;

namespace Quraaa.Application.Features.Authentication.Commands.Register
{
    public record RegisterCommand(
        string PhoneNumber,
        string Password,
        string FirstName,
        string LastName,
        Gender Gender,
        DateOnly DateOfBirth,
        List<string> Interests
    ) : IRequest<AppResult<AuthResponse>>;
}
