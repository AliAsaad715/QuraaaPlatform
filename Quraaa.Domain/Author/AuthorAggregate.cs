using Quraaa.Domain.Shared.Entities;

namespace Quraaa.Domain.Author
{
    public class AuthorAggregate : AggregateRoot
    {
        public string Name { get; private set; } = null!;
        public string? Bio { get; private set; }
        public string? PhotoUrl { get; private set; }
        public DateTime? BirthDate { get; private set; }

        private AuthorAggregate() { }

        public AuthorAggregate(Guid id, string name, string? bio, string? photoUrl, DateTime? birthDate = null)
        {
            Id = id;
            Name = name;
            Bio = bio;
            PhotoUrl = photoUrl;
            BirthDate = birthDate;
        }

        public void UpdateDetails(string name, string? bio, string? photoUrl, DateTime? birthDate, Guid modifiedBy)
        {
            Name = name;
            Bio = bio;
            PhotoUrl = photoUrl;
            BirthDate = birthDate;
            UpdateAudit(modifiedBy);
        }
    }
}
