using Quraaa.Application.Features.Categories.Common;

namespace Quraaa.Application.Features.Libraries.Queries.GetLibraryBooks
{
    public record LibraryBookResponse(
        Guid Id,
        string Title,
        string Author,
        string Description,
        string CoverImageUrl,
        string Language,
        string? Isbn,
        CategoryResponse Category,
        int? Quantity
    );
}
