namespace Quraaa.Application.Features.Authors.Common
{
    public record AuthorResponse(
        Guid Id,
        string Name,
        string? Bio,
        string? PhotoUrl,
        DateTime? BirthDate,
        DateTime CreationTime
    );

    public record AuthorDetailsResponse(
        Guid Id,
        string Name,
        string? Bio,
        string? PhotoUrl,
        DateTime? BirthDate,
        DateTime CreationTime,
        DateTime? LastModificationTime
    );
}
