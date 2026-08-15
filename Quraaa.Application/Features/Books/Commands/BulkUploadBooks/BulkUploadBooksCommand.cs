using MediatR;
using Quraaa.Application.Features.Books.Common;
using Quraaa.Application.Shared.Files;
using Quraaa.Application.Shared.Results;
using Quraaa.Domain.Catalog.Enums;
using Quraaa.Domain.Marketplace.Enums;

namespace Quraaa.Application.Features.Books.Commands.BulkUploadBooks
{
    /// <summary>
    /// Deserialized from the JSON form field "metadata" sent by the frontend.
    /// FolderName must match the subfolder name of the corresponding files.
    /// Price/Quantity/Format describe the commercial listing created for the
    /// uploading library alongside the catalog entry.
    /// </summary>
    public sealed record BookUploadMetadata(
        string FolderName,
        string Title,
        string Author,
        string Description,
        Guid? CategoryId,
        Language Language,
        decimal Price,
        int Quantity,
        ListingFormat Format
    );

    /// <summary>
    /// One complete book unit: the 3 matched files + their metadata.
    /// Assembled by the controller after grouping files by subfolder.
    /// </summary>
    public sealed record BookUploadFileGroup(
        string FolderName,
        IUploadedFile CoverImage,
        IUploadedFile PdfFile,
        IUploadedFile WordFile,
        BookUploadMetadata Metadata
    );

    public sealed record BulkUploadBooksCommand(
        IReadOnlyList<BookUploadFileGroup> Books,
        Guid RequestingUserId
    ) : IRequest<AppResult<BulkUploadBooksResponse>>;
}
