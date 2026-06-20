using Quraaa.Domain.Shared.Entities;

namespace Quraaa.Domain.Catalog
{
    public class BookAggregate : AggregateRoot
    {
        public string Title { get; private set; } = null!;
        public string Author { get; private set; } = null!;
        public string Description { get; private set; } = null!;
        public string CoverImageUrl { get; private set; } = null!;
        public Guid CategoryId { get; private set; }
        public string Language { get; private set; } = null!;
        public string? Isbn { get; private set; }

        private BookAggregate() { }

        public BookAggregate(
            Guid id,
            string title,
            string author,
            string description,
            string coverImageUrl,
            Guid categoryId,
            string language,
            string? isbn = null)
        {
            Id = id;
            Title = title;
            Author = author;
            Description = description;
            CoverImageUrl = coverImageUrl;
            CategoryId = categoryId;
            Language = language;
            Isbn = isbn;
        }

        public void UpdateDetails(
            string title,
            string author,
            string description,
            string coverImageUrl,
            Guid categoryId,
            string language,
            Guid modifiedBy)
        {
            Title = title;
            Author = author;
            Description = description;
            CoverImageUrl = coverImageUrl;
            CategoryId = categoryId;
            Language = language;
            UpdateAudit(modifiedBy);
        }
    }
}