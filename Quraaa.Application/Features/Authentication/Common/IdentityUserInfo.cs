namespace Quraaa.Application.Features.Authentication.Common
{
    public record IdentityUserInfo(
        Guid UserId,
        string PhoneNumber,
        bool PhoneNumberConfirmed
    );
}
