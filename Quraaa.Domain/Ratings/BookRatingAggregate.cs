using Quraaa.Domain.Shared.Entities;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Domain.Ratings
{
    public class BookRatingAggregate : AggregateRoot
    {
        public Guid UserId { get; private set; }
        public Guid BookId { get; private set; }
        public int RatingValue { get; private set; }

        private BookRatingAggregate() { }

        private BookRatingAggregate(Guid id, Guid userId, Guid bookId, int ratingValue)
        {
            Id = id;
            UserId = userId;
            BookId = bookId;
            RatingValue = ratingValue;
        }

        public static BookRatingAggregate Create(Guid userId, Guid bookId, int ratingValue)
        {
            ValidateRating(ratingValue);

            if (userId == Guid.Empty)
            {
                throw new DomainException("User id is required for a rating.");
            }

            if (bookId == Guid.Empty)
            {
                throw new DomainException("Book id is required for a rating.");
            }

            return new BookRatingAggregate(Guid.NewGuid(), userId, bookId, ratingValue);
        }

        public void UpdateRating(int ratingValue, Guid modifiedBy)
        {
            ValidateRating(ratingValue);

            RatingValue = ratingValue;
            UpdateAudit(modifiedBy);
        }

        private static void ValidateRating(int ratingValue)
        {
            if (ratingValue is < 1 or > 5)
            {
                throw new DomainException("Rating must be between 1 and 5.");
            }
        }
    }
}
