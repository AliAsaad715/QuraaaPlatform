namespace Quraaa.Application.Features.AiAssistant.Interfaces
{
    public interface IOpenAiService
    {
        /// <summary>
        /// Returns null on any failure (network error, non-2xx, malformed
        /// response) rather than throwing — mirrors IBookMetadataService's
        /// null-on-failure convention, so callers already know this shape.
        /// </summary>
        Task<string?> GetCompletionAsync(
            string systemPrompt,
            string userMessage,
            int maxTokens,
            CancellationToken cancellationToken = default);
    }
}