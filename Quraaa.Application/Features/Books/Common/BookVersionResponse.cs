using Quraaa.Domain.Catalog.Enums;

namespace Quraaa.Application.Features.Books.Common
{
    /// <summary>One entry of a book's history.</summary>
    public record BookVersionResponse(
        int VersionNumber,
        BookVersionReason Reason,
        int? RevertedFromVersionNumber,
        Guid? ChangedByUserId,
        string Title,
        Guid? AuthorId,
        string Description,
        string CoverImageUrl,
        Guid? CategoryId,
        string Language,
        string? Isbn,
        bool IsCurrent,
        DateTime CreatedAt);
}
