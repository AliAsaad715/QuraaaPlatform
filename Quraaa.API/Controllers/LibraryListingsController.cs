using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.API.Requests.Files;
using Quraaa.API.Requests.Libraries;
using Quraaa.API.Requests.Listings;
using Quraaa.Application.Features.Books.Commands.BulkUploadBooks;
using Quraaa.Application.Features.Books.Common;
using Quraaa.Application.Features.Listings.Commands.AddDigitalBook;
using Quraaa.Application.Features.Listings.Commands.AddPhysicalBook;
using Quraaa.Application.Features.Listings.Commands.ReactivateListing;
using Quraaa.Application.Features.Listings.Commands.RemoveListing;
using Quraaa.Application.Features.Listings.Commands.UpdateListing;
using Quraaa.Application.Features.Listings.Queries.GetLibraryBooks;
using Quraaa.Application.Features.Listings.Queries.GetListingById;
using Quraaa.Application.Features.Listings.Queries.GetMyLibraryListings;
using Quraaa.Application.Shared.Results;
using System.Text.Json;

namespace Quraaa.API.Controllers
{
    /// <summary>
    /// All endpoints require the caller to be an authenticated LibraryOwner
    /// whose library is already approved.
    /// </summary>
    [Authorize(Roles = "LibraryOwner")]
    [ApiController]
    [Route("api/library-admin/listings")]
    public class LibraryListingsController : ApiClientController
    {
        // ── POST /api/library-admin/listings ─────────────────────────────────
        /// <summary>
        /// Adds a physical book to the current user's library.
        /// </summary>
        /// <param name="command">The book data (ISBN, condition, etc.).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The unique identifier (GUID) of the newly added listing.</returns>
        /// <remarks>
        /// ISBN lookup resolution order:
        /// <list type="number">
        /// <item><description>Local Books table (by ISBN).</description></item>
        /// <item><description>Google Books API.</description></item>
        /// </list>
        /// Allowed values for <c>BookCondition</c>:
        /// <c>New = 1, LikeNew = 2, Good = 3, Acceptable = 4.</c>
        /// </remarks>
        [HttpPost]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AddPhysicalBook(
            [FromBody] AddPhysicalBookCommand command,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                command with { RequestingUserId = userId },
                cancellationToken);

            return HandleResult(result);
        }

        // ── POST /api/library-admin/listings/digital ─────────────────────────
        /// <summary>
        /// Adds a digital book to the current user's library.
        /// </summary>
        /// <remarks>
        /// The uploaded digital asset must be a PDF file.
        /// ISBN lookup resolution order:
        /// <list type="number">
        /// <item><description>Local Books table (by ISBN).</description></item>
        /// <item><description>Google Books API.</description></item>
        /// </list>
        /// </remarks>
        [HttpPost("digital")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(AddDigitalBookResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AddDigitalBook(
            [FromForm] AddDigitalBookRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var command = new AddDigitalBookCommand
            {
                RequestingUserId = userId,
                Price = request.Price,
                Isbn = request.Isbn,
                DigitalAsset = new FormFileUploadedFile(request.DigitalAsset)
            };

            var result = await Mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        // ── PUT /api/library-admin/listings/{listingId} ───────────────────────
        /// <summary>
        /// Update price, stock, and/or condition for a listing.
        /// Only fields that are present in the body are updated (partial update).
        /// </summary>
        [HttpPut("{listingId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateListing(
            [FromRoute] Guid listingId,
            [FromBody] UpdateListingCommand command,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                command with
                {
                    ListingId = listingId,
                    RequestingUserId = userId
                },
                cancellationToken);

            return HandleResult(result);
        }

        // ── GET /api/library-admin/listings/me ───────────────────────────────
        /// <summary>
        /// Get a paged list of listings for the authenticated library owner's approved library.
        /// </summary>
        /// <remarks>
        /// The response includes <c>Status</c> so frontend clients can render listing state.
        /// Supported values are <c>Active = 1</c>, <c>OutOfStock = 2</c>, and <c>Removed = 4</c>.
        /// </remarks>
        [HttpGet("me")]
        [ProducesResponseType(typeof(PagedResult<ListingSummaryResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyListings(
            [FromQuery] GetLibraryBooksRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var query = new GetMyLibraryListingsQuery(
                userId,
                request.SearchTerm,
                request.SortBy,
                request.SortDescending,
                request.Status)
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            var result = await Mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        // ── DELETE /api/library-admin/listings/{listingId} ───────────────────
        /// <summary>
        /// Remove a listing from the authenticated library owner's approved library.
        /// </summary>
        [HttpDelete("{listingId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> RemoveListing(
            [FromRoute] Guid listingId,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new RemoveListingCommand(userId, listingId),
                cancellationToken);

            return HandleResult(result);
        }

        // ── PATCH /api/library-admin/listings/{listingId}/activate ─────────────
        /// <summary>
        /// Reactivate a previously removed listing owned by the authenticated library.
        /// </summary>
        [HttpPatch("{listingId:guid}/activate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ReactivateListing(
            [FromRoute] Guid listingId,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new ReactivateListingCommand(userId, listingId),
                cancellationToken);

            return HandleResult(result);
        }

        // ── GET /api/library-admin/listings/{listingId} ───────────────────────
        /// <summary>
        /// Get full listing details (listing + book + category) by listing ID.
        /// </summary>
        [HttpGet("{listingId:guid}")]
        [ProducesResponseType(typeof(ListingDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetListingById(
            [FromRoute] Guid listingId,
            CancellationToken cancellationToken = default)
        {
            var result = await Mediator.Send(
                new GetListingByIdQuery(listingId),
                cancellationToken);

            return HandleResult(result);
        }

        // Web-compatible defaults: camelCase, case-insensitive property matching.
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web);

        /// <summary>
        /// Bulk-upload books from a folder selected with <c>&lt;input webkitdirectory&gt;</c>.
        /// </summary>
        /// <remarks>
        /// **Request shape (multipart/form-data)**
        ///
        /// | Field      | Type               | Description                                        |
        /// |------------|--------------------|----------------------------------------------------|
        /// | metadata   | string (JSON)      | JSON array of <c>BookUploadMetadata</c> objects.   |
        /// | files      | IFormFileCollection| All files from the selected folder (flat list).    |
        ///
        /// **Grouping contract**
        ///
        /// Each file's <c>FileName</c> must contain its subfolder as the first path segment,
        /// e.g. <c>BookSubfolder1/cover.jpg</c>. The subfolder name is matched against the
        /// <c>FolderName</c> field in the JSON metadata array.
        ///
        /// **Per-subfolder requirements (exactly 3 files)**
        /// - One image  : .jpg / .jpeg / .png / .webp  → stored as <c>CoverImageUrl</c>
        /// - One PDF    : .pdf                          → stored as the book's canonical PDF
        /// - One Word   : .doc / .docx                 → stored as the book's canonical Word document
        ///
        /// Each book also gets a listing created for the calling library, using the
        /// metadata's <c>Price</c>, <c>Quantity</c> (mapped to listing stock), and
        /// <c>Format</c>. Digital listings reuse the uploaded PDF as their asset;
        /// physical listings default to <c>BookCondition.New</c>.
        /// Allowed values for <c>Format</c>: <c>Digital = 1, Physical = 2</c>.
        ///
        /// **Metadata JSON example**
        /// ```json
        /// [
        ///   {
        ///     "folderName": "BookSubfolder1",
        ///     "title": "Clean Architecture",
        ///     "author": "Robert C. Martin",
        ///     "description": "A practical guide …",
        ///     "categoryId": "11111111-1111-1111-1111-111111111103",
        ///     "language": "en",
        ///     "price": 19.99,
        ///     "quantity": 10,
        ///     "format": 2
        ///   }
        /// ]
        /// ```
        /// </remarks>
        [HttpPost("bulk-upload")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(500 * 1024 * 1024)]              // 500 MB total request
        [RequestFormLimits(MultipartBodyLengthLimit = 500 * 1024 * 1024)]
        [ProducesResponseType(typeof(BulkUploadBooksResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> BulkUpload(
            [FromForm(Name = "metadata")] string metadataJson,
            IFormFileCollection files,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
                return InvalidUserIdResult();

            // ── Step 1: Deserialize metadata ─────────────────────────────────────
            List<BookUploadMetadata>? metadataList;
            try
            {
                metadataList = JsonSerializer.Deserialize<List<BookUploadMetadata>>(metadataJson, JsonOptions);
            }
            catch (JsonException)
            {
                return BadRequest(new { type = "ValidationFailure", title = "The 'metadata' field is not valid JSON." });
            }

            if (metadataList is null || metadataList.Count == 0)
                return BadRequest(new { type = "ValidationFailure", title = "The 'metadata' JSON array cannot be empty." });

            if (files.Count == 0)
                return BadRequest(new { type = "ValidationFailure", title = "No files were uploaded." });

            // ── Step 2: Build metadata lookup (FolderName → metadata) ────────────
            // Duplicate folder names in the metadata array are silently de-duped (first wins).
            var metaByFolder = metadataList
                .GroupBy(m => m.FolderName.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            // ── Step 3: Group uploaded files by first path segment ───────────────
            // webkitdirectory sends file paths as  "SubfolderName/file.ext".
            // IFormFile.FileName contains that relative path.
            var fileGroups = files
                .Where(f => !string.IsNullOrEmpty(GetFolderName(f.FileName)))
                .GroupBy(f => GetFolderName(f.FileName), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            // ── Step 4: Match file groups to metadata ────────────────────────────
            var bookGroups = new List<BookUploadFileGroup>(metadataList.Count);
            var errors = new List<string>();

            foreach (var (folderName, folderFiles) in fileGroups)
            {
                if (!metaByFolder.TryGetValue(folderName, out var metadata))
                {
                    errors.Add($"Folder '{folderName}': no matching metadata entry found.");
                    continue;
                }

                var coverFile = folderFiles.FirstOrDefault(IsImageFile);
                var pdfFile = folderFiles.FirstOrDefault(IsPdfFile);
                var wordFile = folderFiles.FirstOrDefault(IsWordFile);

                if (coverFile is null) { errors.Add($"Folder '{folderName}': missing cover image (.jpg/.jpeg/.png/.webp)."); continue; }
                if (pdfFile is null) { errors.Add($"Folder '{folderName}': missing PDF file (.pdf)."); continue; }
                if (wordFile is null) { errors.Add($"Folder '{folderName}': missing Word document (.doc/.docx)."); continue; }

                bookGroups.Add(new BookUploadFileGroup(
                    FolderName: folderName,
                    CoverImage: new FormFileUploadedFile(coverFile),
                    PdfFile: new FormFileUploadedFile(pdfFile),
                    WordFile: new FormFileUploadedFile(wordFile),
                    Metadata: metadata));
            }

            // Report every grouping error before doing any I/O.
            if (errors.Count > 0)
            {
                return BadRequest(new
                {
                    type = "ValidationFailure",
                    title = "File grouping failed. Correct the errors and retry.",
                    errors
                });
            }

            // ── Step 5: Dispatch the command ─────────────────────────────────────
            var command = new BulkUploadBooksCommand(bookGroups, userId);
            var result = await Mediator.Send(command, cancellationToken);

            return HandleResult(result, data => StatusCode(StatusCodes.Status201Created, data));
        }

        // ── File helpers ─────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the first path segment (subfolder name) from a webkitdirectory filename.
        /// "Book1/cover.jpg" → "Book1". Files without a slash return "".
        /// </summary>
        private static string GetFolderName(string fileName)
        {
            // Normalize Windows-style separators sent by some browsers.
            var normalized = fileName.Replace('\\', '/');
            var slashIndex = normalized.IndexOf('/');
            return slashIndex > 0 ? normalized[..slashIndex] : string.Empty;
        }

        private static bool IsImageFile(IFormFile f)
        {
            var ext = Path.GetExtension(f.FileName).ToLowerInvariant();
            return ext is ".jpg" or ".jpeg" or ".png" or ".webp";
        }

        private static bool IsPdfFile(IFormFile f) =>
            Path.GetExtension(f.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase);

        private static bool IsWordFile(IFormFile f)
        {
            var ext = Path.GetExtension(f.FileName).ToLowerInvariant();
            return ext is ".doc" or ".docx";
        }
    }
}
