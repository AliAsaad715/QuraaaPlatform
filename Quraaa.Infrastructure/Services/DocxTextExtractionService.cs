using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Shared.Files;
using System.Text;

namespace Quraaa.Infrastructure.Services
{
    /// <summary>
    /// Wraps DocumentFormat.OpenXml behind the Application layer's
    /// IDocxTextExtractionService abstraction, so the third-party Word-document
    /// dependency stays confined to this Infrastructure project.
    /// </summary>
    public sealed class DocxTextExtractionService : IDocxTextExtractionService
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<DocxTextExtractionService> _logger;

        public DocxTextExtractionService(
            IFileStorageService fileStorageService,
            ILogger<DocxTextExtractionService> logger)
        {
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        public Task<string?> ExtractDocxTextAsync(
            string relativePath,
            int maxCharacters,
            CancellationToken cancellationToken = default)
        {
            if (!_fileStorageService.TryGetPhysicalPath(relativePath, out var physicalPath))
                return Task.FromResult<string?>(null);

            // OpenXml's API is synchronous and can be CPU-bound for large files, so this
            // is offloaded to avoid tying up a request-handling thread while it runs.
            return Task.Run(
                () => ExtractText(physicalPath, maxCharacters, cancellationToken),
                cancellationToken);
        }

        private string? ExtractText(string physicalPath, int maxCharacters, CancellationToken cancellationToken)
        {
            try
            {
                // Canonical book files are shared across every purchaser, so concurrent
                // summarize requests can open the same file at once — FileShare.Read keeps
                // those reads from colliding and prevents locking the file against writers.
                using var stream = new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var wordDocument = WordprocessingDocument.Open(stream, false);

                var body = wordDocument.MainDocumentPart?.Document?.Body;
                if (body is null)
                    return null;

                var builder = new StringBuilder(Math.Min(maxCharacters, 4096));

                foreach (var paragraph in body.Elements<Paragraph>())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var paragraphText = paragraph.InnerText;
                    if (string.IsNullOrWhiteSpace(paragraphText))
                        continue;

                    if (builder.Length > 0)
                        builder.Append('\n');

                    builder.Append(paragraphText);

                    if (builder.Length >= maxCharacters)
                        break;
                }

                if (builder.Length == 0)
                    return null;

                return builder.Length > maxCharacters
                    ? builder.ToString(0, maxCharacters)
                    : builder.ToString();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Malformed .docx, unsupported package structure, empty body, etc. — the
                // caller falls back to metadata-only context rather than failing the
                // whole request over an unreadable file.
                _logger.LogWarning(ex, "Failed to extract text from DOCX at {PhysicalPath}", physicalPath);
                return null;
            }
        }
    }
}
