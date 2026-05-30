namespace Quraaa.Domain.Shared.Entities
{
    public interface IDomainEvents { }

    public abstract class AggregateRoot : AuditableEntity
    {
        // --- Domain Events ---
        private readonly List<IDomainEvents> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvents> DomainEvents => _domainEvents;

        protected void AddDomainEvent(IDomainEvents eventItem) => _domainEvents.Add(eventItem);
        public void ClearDomainEvents() => _domainEvents.Clear();

        // --- Audit(User Tracking) ---
        public Guid? LastModifiedBy { get; protected set; }

        // --- Soft Delete ---
        public bool IsDeleted { get; protected set; }
        public DateTime? DeleationTime { get; protected set; }
        public Guid? DeletedBy {  get; protected set; }

        public void Delete(Guid deletedBy)
        {
            IsDeleted = true;
            DeleationTime = DateTime.UtcNow;
            DeletedBy = deletedBy;
        }

        public void UpdateAudit(Guid modifiedBy)
        {
            LastModifiedBy = modifiedBy;
            UpdateModificationTime();
        }
    }
}
