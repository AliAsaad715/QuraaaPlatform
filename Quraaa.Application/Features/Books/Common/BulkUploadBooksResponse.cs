using Quraaa.Domain.Catalog.Enums;
using Quraaa.Domain.Marketplace.Enums;

namespace Quraaa.Application.Features.Books.Common
{
    public sealed record BulkUploadBooksResponse(
        int TotalUploaded,
        int TotalSkipped,
        IReadOnlyList<BookUploadResult> Results,
        IReadOnlyList<SkippedBookResult> Skipped
    );

    public sealed record BookUploadResult(
        Guid BookId,
        string Title,
        string CoverImageUrl,
        string PdfUrl,
        string WordDocUrl,
        Guid ListingId,
        decimal Price,
        int? Stock,
        ListingFormat Format
    );

    /// <summary>
    /// Describes a book that was not inserted because it already exists or was a batch duplicate.
    /// </summary>
    public sealed record SkippedBookResult(
        string FolderName,
        string Title,
        string Author,
        Language Language,
        string Reason
    );
}
