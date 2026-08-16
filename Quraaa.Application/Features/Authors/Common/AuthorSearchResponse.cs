namespace Quraaa.Application.Features.Authors.Common
{
    public record AuthorSearchResponse(Guid Id, string Name, string? PhotoUrl, int TotalBooksCount);
}
