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

        public async Task<string?> ExtractDocxTextAsync(
            string relativePath,
            int maxCharacters,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await using var stream = await _fileStorageService.OpenReadAsync(
                    relativePath,
                    cancellationToken);
                if (stream is null)
                    return null;

                // OpenXml's API is synchronous and can be CPU-bound for large files, so
                // parsing is offloaded after the provider has supplied a seekable stream.
                return await Task.Run(
                    () => ExtractText(stream, maxCharacters, cancellationToken),
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
                    "Failed to open DOCX storage reference {StoredReference} for text extraction.",
                    relativePath);
                return null;
            }
        }

        private string? ExtractText(Stream stream, int maxCharacters, CancellationToken cancellationToken)
        {
            try
            {
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
                _logger.LogWarning(ex, "Failed to extract text from DOCX.");
                return null;
            }
        }
    }
}
