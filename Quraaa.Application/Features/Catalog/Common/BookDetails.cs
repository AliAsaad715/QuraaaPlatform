using Quraaa.Application.Features.Categories.Common;
using Quraaa.Domain.Catalog.Enums;

namespace Quraaa.Application.Features.Catalog.Common
{
    public record BookDetails(
        Guid BookId,
        string Title,
        string? Author,
        string Description,
        string CoverImageUrl,
        Language Language,
        string? Isbn,
        CategoryResponse? Category
    );
}