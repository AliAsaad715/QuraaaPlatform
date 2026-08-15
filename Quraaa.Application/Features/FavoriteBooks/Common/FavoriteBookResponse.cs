using Quraaa.Domain.Catalog.Enums;

namespace Quraaa.Application.Features.FavoriteBooks.Common
{
    public record FavoriteBookResponse(
        Guid FavoriteId,
        Guid BookId,
        string Title,
        string? Author,
        string Description,
        string CoverImageUrl,
        Guid? CategoryId,
        Language Language,
        string? Isbn,
        DateTime FavoritedAt);
}
