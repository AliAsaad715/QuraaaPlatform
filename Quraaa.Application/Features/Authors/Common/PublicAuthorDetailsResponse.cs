namespace Quraaa.Application.Features.Authors.Common
{
    public sealed record PublicAuthorDetailsResponse(
        Guid Id,
        string Name,
        string? Bio,
        string? PhotoUrl,
        DateTime? BirthDate);
}
