namespace Quraaa.API.Requests.Admin
{
    /// <summary>The records a bulk action applies to.</summary>
    public class BulkIdsRequest
    {
        public IReadOnlyCollection<Guid> Ids { get; set; } = [];
    }

    /// <summary>A bulk activation change.</summary>
    public class BulkActivationRequest : BulkIdsRequest
    {
        /// <summary>True deactivates the records; false brings them back.</summary>
        public bool Deactivate { get; set; } = true;
    }
}
