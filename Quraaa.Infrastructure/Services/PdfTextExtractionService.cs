using Microsoft.Extensions.Logging;
using Quraaa.Application.Shared.Files;
using UglyToad.PdfPig;

namespace Quraaa.Infrastructure.Services
{
    /// <summary>
    /// Wraps PdfPig (a pure-managed PDF library) behind the Application layer's
    /// IDocumentTextExtractionService abstraction, so the third-party PDF dependency
    /// stays confined to this Infrastructure project.
    /// </summary>
    public sealed class PdfTextExtractionService : IDocumentTextExtractionService
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<PdfTextExtractionService> _logger;

        public PdfTextExtractionService(
            IFileStorageService fileStorageService,
            ILogger<PdfTextExtractionService> logger)
        {
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        public Task<string?> ExtractPageTextAsync(
            string relativePath,
            int pageNumber,
            CancellationToken cancellationToken = default)
        {
            if (!_fileStorageService.TryGetPhysicalPath(relativePath, out var physicalPath))
                return Task.FromResult<string?>(null);

            // PdfPig's API is synchronous and can be CPU-bound for large files (these
            // can be up to 100 MB — see BulkUploadBooksCommandValidator), so this is
            // offloaded to avoid tying up a request-handling thread while it runs.
            return Task.Run(
                () => ExtractPageText(physicalPath, pageNumber, cancellationToken),
                cancellationToken);
        }

        private string? ExtractPageText(string physicalPath, int pageNumber, CancellationToken cancellationToken)
        {
            try
            {
                // Canonical book files are shared across every purchaser, so concurrent
                // translation requests can open the same file at once — FileShare.Read
                // (rather than PdfPig's default exclusive-ish path-based open) keeps
                // those reads from colliding.
                using var stream = new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var document = PdfDocument.Open(stream);

                cancellationToken.ThrowIfCancellationRequested();

                if (pageNumber < 1 || pageNumber > document.NumberOfPages)
                    return null;

                var pageText = document.GetPage(pageNumber).Text;
                return string.IsNullOrWhiteSpace(pageText) ? null : pageText;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Malformed PDF, unsupported encryption, scanned-image-only content,
                // etc. — the caller surfaces a business error rather than crashing
                // over an unreadable file.
                _logger.LogWarning(ex, "Failed to extract text from page {PageNumber} of PDF at {PhysicalPath}", pageNumber, physicalPath);
                return null;
            }
        }
    }
}
