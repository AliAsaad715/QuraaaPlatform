using Quraaa.Domain.Shared.Entities;

namespace Quraaa.Domain.Catalog
{
    public class BookAggregate : AggregateRoot
    {
        public string Title { get; private set; } = null!;
        public string Author { get; private set; } = null!;
        public string Description { get; private set; } = null!;
        public string CoverImageUrl { get; private set; } = null!;
        public Guid? CategoryId { get; private set; }
        public string Language { get; private set; } = null!;
        public string? Isbn { get; private set; }

        // Master digital files for this book, captured once at the catalog level
        // (e.g. via bulk upload) so every merchant listing this book can reuse them
        // instead of each merchant re-uploading its own copy.
        public string? CanonicalPdfUrl { get; private set; }
        public string? CanonicalWordDocUrl { get; private set; }

        private BookAggregate() { }

        public BookAggregate(
            Guid id,
            string title,
            string author,
            string description,
            string coverImageUrl,
            string language,
            Guid? categoryId = null,
            string? isbn = null,
            string? canonicalPdfUrl = null,
            string? canonicalWordDocUrl = null)
        {
            Id = id;
            Title = title;
            Author = author;
            Description = description;
            CoverImageUrl = coverImageUrl;
            CategoryId = categoryId;
            Language = language;
            Isbn = isbn;
            CanonicalPdfUrl = canonicalPdfUrl;
            CanonicalWordDocUrl = canonicalWordDocUrl;
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