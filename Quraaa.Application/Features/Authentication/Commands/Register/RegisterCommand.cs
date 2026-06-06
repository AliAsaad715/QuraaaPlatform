using MediatR;
using Quraaa.Application.Features.Authentication.Common;
using Quraaa.Application.Shared.Results;
using Quraaa.Domain.User.Enums;

public record RegisterCommand(
    string FirstName,
    string LastName,
    string PhoneNumber,
    string Password,
    Gender Gender,
    DateOnly DateOfBirth,
    List<string> Interests
) : IRequest<AppResult<AuthResponse>>;