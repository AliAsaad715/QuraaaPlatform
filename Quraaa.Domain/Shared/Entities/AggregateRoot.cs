using Quraaa.Domain.Shared.Events;

namespace Quraaa.Domain.Shared.Entities
{
    public abstract class AggregateRoot : AuditableEntity
    {
        // --- Domain Events ---
        private readonly List<IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

        protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
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

        /// <summary>
        /// Undoes <see cref="Delete"/>. Soft deletion is how the platform
        /// deactivates a record, so it has to be reversible; permanent removal
        /// is a separate, guarded operation.
        /// </summary>
        public void Restore(Guid restoredBy)
        {
            if (!IsDeleted)
            {
                return;
            }

            IsDeleted = false;
            DeleationTime = null;
            DeletedBy = null;
            UpdateAudit(restoredBy);
        }

        public void UpdateAudit(Guid modifiedBy)
        {
            LastModifiedBy = modifiedBy;
            UpdateModificationTime();
        }
    }
}
