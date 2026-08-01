namespace Quraaa.Persistence.Data
{
    public sealed class ConsumedRefreshToken
    {
        private ConsumedRefreshToken()
        {
        }

        public ConsumedRefreshToken(
            Guid userId,
            Guid familyId,
            string tokenHash,
            DateTime consumedAtUtc,
            DateTime expiresAtUtc)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            FamilyId = familyId;
            TokenHash = tokenHash;
            ConsumedAtUtc = consumedAtUtc;
            ExpiresAtUtc = expiresAtUtc;
        }

        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public Guid FamilyId { get; private set; }
        public string TokenHash { get; private set; } = null!;
        public DateTime ConsumedAtUtc { get; private set; }
        public DateTime ExpiresAtUtc { get; private set; }
    }
}
