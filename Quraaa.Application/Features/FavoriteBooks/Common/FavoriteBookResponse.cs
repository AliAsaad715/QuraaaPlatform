namespace Quraaa.Application.Features.FavoriteBooks.Common
{
    public record FavoriteBookResponse(
        Guid FavoriteId,
        Guid BookId,
        string Title,
        string Author,
        string Description,
        string CoverImageUrl,
        Guid CategoryId,
        string Language,
        string? Isbn,
        DateTime FavoritedAt);
}
