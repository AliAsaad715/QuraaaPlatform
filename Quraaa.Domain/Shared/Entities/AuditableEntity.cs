namespace Quraaa.Domain.Shared.Entities
{
    public abstract class AuditableEntity : Entity
    {
        public DateTime CreationTime { get; protected set; } = DateTime.UtcNow;
        public DateTime? LastModificationTime { get; protected set; }

        public void UpdateModificationTime()
        {
            LastModificationTime = DateTime.UtcNow;
        }
    }
}
