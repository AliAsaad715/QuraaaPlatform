namespace Quraaa.Application.Shared.Files
{
    /// <summary>
    /// Extracts plain text from a single page of a stored PDF so its content can be
    /// used as AI prompt context (e.g. translating the page the reader is currently
    /// on). Takes the same root-relative paths used elsewhere for file storage/access
    /// (see <see cref="IFileStorageService"/>) — never a physical path.
    /// </summary>
    public interface IDocumentTextExtractionService
    {
        /// <summary>
        /// Extracts the text of page <paramref name="pageNumber"/> (1-based) from the
        /// PDF at <paramref name="relativePath"/>. Returns null if the file doesn't
        /// exist, isn't a readable PDF, the page number is out of range, or that page
        /// has no extractable text (e.g. a scanned page image with no text layer) —
        /// callers should surface this as a business error rather than crash.
        /// </summary>
        Task<string?> ExtractPageTextAsync(
            string relativePath,
            int pageNumber,
            CancellationToken cancellationToken = default);
    }
}
