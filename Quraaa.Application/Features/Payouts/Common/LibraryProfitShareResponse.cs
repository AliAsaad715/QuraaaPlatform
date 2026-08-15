using Quraaa.Domain.Library;

namespace Quraaa.Application.Features.Payouts.Common
{
    /// <summary>
    /// A library's profit-share setting as seen by administrators.
    /// </summary>
    /// <param name="LibraryId">The library.</param>
    /// <param name="LibraryName">The library's display name.</param>
    /// <param name="ProfitSharePercent">
    /// The percentage of the library's gross sales paid out to its owner on
    /// each paid order (the platform keeps the remainder).
    /// </param>
    /// <param name="DefaultProfitSharePercent">
    /// The percentage new libraries start with until an administrator
    /// changes it.
    /// </param>
    /// <param name="LastModifiedAtUtc">When the library record was last changed.</param>
    public record LibraryProfitShareResponse(
        Guid LibraryId,
        string LibraryName,
        decimal ProfitSharePercent,
        decimal DefaultProfitSharePercent,
        DateTime? LastModifiedAtUtc)
    {
        public static LibraryProfitShareResponse From(LibraryAggregate library)
        {
            return new LibraryProfitShareResponse(
                library.Id,
                library.LibraryName,
                library.ProfitSharePercent,
                LibraryAggregate.DefaultProfitSharePercent,
                library.LastModificationTime);
        }
    }
}
