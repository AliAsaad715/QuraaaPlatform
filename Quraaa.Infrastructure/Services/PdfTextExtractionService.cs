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

        public async Task<string?> ExtractPageTextAsync(
            string relativePath,
            int pageNumber,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await using var stream = await _fileStorageService.OpenReadAsync(
                    relativePath,
                    cancellationToken);
                if (stream is null)
                    return null;

                // PdfPig's API is synchronous and can be CPU-bound for large files, so
                // parsing is offloaded after the provider has supplied a seekable stream.
                return await Task.Run(
                    () => ExtractPageText(stream, pageNumber, cancellationToken),
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to open PDF storage reference {StoredReference} for text extraction.",
                    relativePath);
                return null;
            }
        }

        private string? ExtractPageText(Stream stream, int pageNumber, CancellationToken cancellationToken)
        {
            try
            {
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
                _logger.LogWarning(ex, "Failed to extract text from PDF page {PageNumber}.", pageNumber);
                return null;
            }
        }
    }
}
