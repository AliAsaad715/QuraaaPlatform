using Quraaa.Domain.Shared.Entities;

namespace Quraaa.Domain.Favorites
{
    public class FavoriteBookAggregate : AggregateRoot
    {
        public Guid UserId { get; private set; }
        public Guid BookId { get; private set; }

        private FavoriteBookAggregate() { }

        private FavoriteBookAggregate(Guid id, Guid userId, Guid bookId)
        {
            Id = id;
            UserId = userId;
            BookId = bookId;
        }

        public static FavoriteBookAggregate Create(Guid userId, Guid bookId)
        {
            return new FavoriteBookAggregate(Guid.NewGuid(), userId, bookId);
        }
    }
}
