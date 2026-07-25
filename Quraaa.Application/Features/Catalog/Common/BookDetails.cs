using Quraaa.Application.Features.Categories.Common;

namespace Quraaa.Application.Features.Catalog.Common
{
    public record BookDetails(
        Guid BookId,
        string Title,
        string Author,
        string Description,
        string CoverImageUrl,
        string Language,
        string? Isbn,
        CategoryResponse? Category
    );
}