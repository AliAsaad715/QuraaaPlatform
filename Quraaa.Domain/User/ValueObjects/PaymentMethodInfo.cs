using Quraaa.Domain.Shared.Entities;

namespace Quraaa.Domain.User.ValueObjects
{
    public class PaymentMethodInfo : ValueObjectRoot
    {
        public string GatewayCustomerId { get; init; } = null!;
        public string CardBrand { get; init; } = null!;
        public string LastFourDigits { get; init; } = null!;

        private PaymentMethodInfo() { }

        public PaymentMethodInfo(string gatewayCustomerId, string cardBrand, string lastFourDigits)
        {
            GatewayCustomerId = gatewayCustomerId;
            CardBrand = cardBrand;
            LastFourDigits = lastFourDigits;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return GatewayCustomerId;
            yield return CardBrand;
            yield return LastFourDigits;
        }
    }
}
