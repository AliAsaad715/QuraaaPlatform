namespace Quraaa.Domain.Payouts.Enums
{
    public enum SellerPayoutStatus
    {
        /// <summary>Waiting to be transferred to the library's wallet.</summary>
        Pending = 1,

        /// <summary>The transfer to the library's wallet succeeded.</summary>
        Paid = 2,

        /// <summary>All transfer attempts were exhausted; needs manual review.</summary>
        Failed = 3,

        /// <summary>
        /// The library's profit share of this order rounded to zero minor
        /// units, so there was nothing to transfer. Recorded for audit only.
        /// </summary>
        NoAmountDue = 4,
    }
}
