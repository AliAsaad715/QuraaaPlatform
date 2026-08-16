namespace Quraaa.Application.Features.Admin.Common
{
    /// <summary>
    /// One reason a record cannot be permanently removed: something still
    /// references it.
    /// </summary>
    /// <param name="Reference">What still points at the record, e.g. "Listings".</param>
    /// <param name="Count">How many rows.</param>
    public record EntityDeletionBlocker(string Reference, int Count);
}
