namespace Quraaa.Application.Features.Payments.Common
{
    public static class PaymentCheckoutLimits
    {
        public const int MaximumLineItems = 100;
        public const int MaximumQuantityPerLine = 999_999;
        public const long MinimumUsdChargeMinor = 50;
        public const long MaximumUsdAmountMinor = 99_999_999;
    }
}
