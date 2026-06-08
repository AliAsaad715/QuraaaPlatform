using Quraaa.Domain.Shared.Entities;
using Quraaa.Domain.Library.Enums;

namespace Quraaa.Domain.Library
{
    public class LibraryAggregate : AggregateRoot
    {
        public string LibraryName { get; private set; } = null!;
        public string Location { get; private set; } = null!;
        public string LibraryImage { get; private set; } = null!;
        public string HeaderImage { get; private set; } = null!;
        public string Email { get; private set; } = null!;
        public Guid UserId { get; private set; }
        public LibraryApprovalStatus ApprovalStatus { get; private set; }

        private LibraryAggregate() { }

        public LibraryAggregate(
            Guid id,
            string libraryName,
            string location,
            string libraryImage,
            string headerImage,
            string email,
            Guid userId)
        {
            Id = id;
            LibraryName = libraryName;
            Location = location;
            LibraryImage = libraryImage;
            HeaderImage = headerImage;
            Email = email;
            UserId = userId;
            ApprovalStatus = LibraryApprovalStatus.Pending;
        }
    }
}
