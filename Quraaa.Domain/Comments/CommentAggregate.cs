using Quraaa.Domain.Shared.Entities;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Domain.Comments
{
    public class CommentAggregate : AggregateRoot
    {
        public Guid UserId { get; private set; }
        public Guid BookId { get; private set; }
        public string Content { get; private set; } = null!;

        private CommentAggregate() { }

        private CommentAggregate(Guid id, Guid userId, Guid bookId, string content)
        {
            Id = id;
            UserId = userId;
            BookId = bookId;
            Content = content;
        }

        public static CommentAggregate Create(Guid userId, Guid bookId, string content)
        {
            if (userId == Guid.Empty)
            {
                throw new DomainException("User id is required for a comment.");
            }

            if (bookId == Guid.Empty)
            {
                throw new DomainException("Book id is required for a comment.");
            }

            ValidateContent(content);

            return new CommentAggregate(Guid.NewGuid(), userId, bookId, content.Trim());
        }

        public void UpdateContent(string content, Guid modifiedBy)
        {
            ValidateContent(content);

            Content = content.Trim();
            UpdateAudit(modifiedBy);
        }

        private static void ValidateContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new DomainException("Comment content is required.");
            }

            if (content.Trim().Length > 2000)
            {
                throw new DomainException("Comment content cannot exceed 2000 characters.");
            }
        }
    }
}
