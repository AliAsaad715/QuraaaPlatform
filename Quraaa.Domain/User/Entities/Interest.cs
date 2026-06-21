using Quraaa.Domain.Shared.Entities;

namespace Quraaa.Domain.User.Entities
{
    public class Interest : Entity
    {
        public Guid UserId { get; private set; }
        public Guid CategoryId { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private Interest() { }

        public Interest(Guid userId, Guid categoryId)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            CategoryId = categoryId;
            CreatedAt = DateTime.UtcNow;
        }
    }
}