namespace Quraaa.Application.Features.Purchases.Common
{
    /// <summary>
    /// Owner and book snapshot for a purchase, used to authorize AI-assistant
    /// requests (Summarize/Explain) against the book the caller actually bought,
    /// and to build the AI prompt context from that book without a second lookup.
    /// </summary>
    public sealed record PurchaseBookContext(
        Guid UserId,
        Guid BookId,
        string Title,
        string Author,
        string Description,
        string? CanonicalPdfUrl,
        string? CanonicalWordDocUrl);
}
