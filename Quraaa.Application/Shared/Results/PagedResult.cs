namespace Quraaa.Application.Shared.Results
{
    public record PagedResult<T>(
        IReadOnlyCollection<T> Items,
        int PageNumber,
        int PageSize,
        int TotalCount
    )
    {
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasNextPage => PageNumber < TotalPages;
        public bool HasPreviousPage => PageNumber > 1;
    }
}