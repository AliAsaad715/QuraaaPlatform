using Quraaa.Domain.Shared.Entities;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Domain.Reviews
{
    public class BookReviewAggregate : AggregateRoot
    {
        public Guid UserId { get; private set; }
        public Guid BookId { get; private set; }
        public int Score { get; private set; }
        public string Content { get; private set; } = null!;

        private BookReviewAggregate() { }

        private BookReviewAggregate(Guid id, Guid userId, Guid bookId, int score, string content)
        {
            Id = id;
            UserId = userId;
            BookId = bookId;
            Score = score;
            Content = content;
        }

        public static BookReviewAggregate Create(Guid userId, Guid bookId, int score, string content)
        {
            if (userId == Guid.Empty)
            {
                throw new DomainException("User id is required for a review.");
            }

            if (bookId == Guid.Empty)
            {
                throw new DomainException("Book id is required for a review.");
            }

            ValidateScore(score);
            ValidateContent(content);

            return new BookReviewAggregate(Guid.NewGuid(), userId, bookId, score, content.Trim());
        }

        public void UpdateReview(int score, string content, Guid modifiedBy)
        {
            ValidateScore(score);
            ValidateContent(content);

            Score = score;
            Content = content.Trim();
            UpdateAudit(modifiedBy);
        }

        private static void ValidateScore(int score)
        {
            if (score is < 1 or > 5)
            {
                throw new DomainException("Score must be between 1 and 5.");
            }
        }

        private static void ValidateContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new DomainException("Review content is required.");
            }

            if (content.Trim().Length > 1000)
            {
                throw new DomainException("Review content cannot exceed 1000 characters.");
            }
        }
    }
}
