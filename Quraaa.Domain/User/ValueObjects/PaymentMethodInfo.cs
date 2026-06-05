using Quraaa.Domain.Shared.Entities;

namespace Quraaa.Domain.User.ValueObjects
{
    public class PaymentMethodInfo : ValueObjectRoot
    {
        public string GatewayCustomerId { get; init; }
        public string CardBrand { get; init; }
        public string LastFourDigits { get; init; }

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
