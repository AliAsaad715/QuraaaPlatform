namespace Quraaa.Application.Shared.Files
{
    /// <summary>
    /// Extracts plain text from a stored Word (.docx) document so its content can be
    /// used as AI prompt context. Used for whole-document context (e.g. summarizing a
    /// book's opening) rather than a single page — see <see cref="IDocumentTextExtractionService"/>
    /// for page-scoped PDF extraction. Takes the same opaque references used elsewhere
    /// for file storage/access (see <see cref="IFileStorageService"/>) — never a physical path.
    /// </summary>
    public interface IDocxTextExtractionService
    {
        /// <summary>
        /// Extracts text from the .docx body at <paramref name="relativePath"/>, stopping
        /// once <paramref name="maxCharacters"/> characters have been collected. Returns
        /// null if the file doesn't exist, isn't a readable .docx, or has no extractable
        /// text — callers should fall back to other context (title/author/description)
        /// rather than fail the request.
        /// </summary>
        Task<string?> ExtractDocxTextAsync(
            string relativePath,
            int maxCharacters,
            CancellationToken cancellationToken = default);
    }
}
