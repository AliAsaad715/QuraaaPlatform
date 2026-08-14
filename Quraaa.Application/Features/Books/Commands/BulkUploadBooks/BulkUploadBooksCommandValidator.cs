using FluentValidation;
using Quraaa.Application.Shared.Files;

namespace Quraaa.Application.Features.Books.Commands.BulkUploadBooks
{
    public sealed class BulkUploadBooksCommandValidator : AbstractValidator<BulkUploadBooksCommand>
    {
        private const int MaxBooksPerBatch = 100;

        public BulkUploadBooksCommandValidator()
        {
            RuleFor(x => x.Books)
                .NotEmpty()
                    .WithMessage("At least one book must be provided.")
                .Must(b => b.Count <= MaxBooksPerBatch)
                    .WithMessage($"Cannot upload more than {MaxBooksPerBatch} books per request.");

            RuleForEach(x => x.Books).SetValidator(new BookUploadFileGroupValidator());
        }
    }

    internal sealed class BookUploadFileGroupValidator : AbstractValidator<BookUploadFileGroup>
    {
        private const long MaxImageBytes = 5L  * 1024 * 1024;   // 5 MB
        private const long MaxPdfBytes   = 100L * 1024 * 1024;  // 100 MB
        private const long MaxWordBytes  = 50L  * 1024 * 1024;  // 50 MB

        private static readonly IReadOnlySet<string> AllowedImageExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

        private static readonly IReadOnlySet<string> AllowedImageContentTypes =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { "image/jpeg", "image/jpg", "image/png", "image/webp" };

        private static readonly IReadOnlySet<string> AllowedWordExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".doc", ".docx" };

        private static readonly IReadOnlySet<string> AllowedPdfContentTypes =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { "application/pdf", "application/octet-stream" };

        private static readonly IReadOnlySet<string> AllowedWordContentTypes =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "application/msword",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "application/octet-stream"
            };

        public BookUploadFileGroupValidator()
        {
            RuleFor(x => x.Metadata.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(250).WithMessage("Title must not exceed 250 characters.");

            RuleFor(x => x.Metadata.Author)
                .NotEmpty().WithMessage("Author is required.")
                .MaximumLength(150).WithMessage("Author must not exceed 150 characters.");

            RuleFor(x => x.Metadata.Description)
                .NotEmpty().WithMessage("Description is required.")
                .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");

            RuleFor(x => x.Metadata.Language)
                .NotEmpty().WithMessage("Language is required.")
                .MaximumLength(20).WithMessage("Language must not exceed 20 characters.");

            RuleFor(x => x.Metadata.Price)
                .GreaterThan(0m).WithMessage("Price must be greater than zero.");

            RuleFor(x => x.Metadata.Quantity)
                .GreaterThanOrEqualTo(1).WithMessage("Quantity must be at least 1.");

            RuleFor(x => x.Metadata.Format)
                .IsInEnum().WithMessage("Format must be a valid listing format (Digital or Physical).");

            RuleFor(x => x.CoverImage)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("Cover image is required.")
                .Must(f => f!.Length > 0 && f.Length <= MaxImageBytes)
                    .WithMessage("Cover image must be between 1 byte and 5 MB.")
                .Must(f => AllowedImageExtensions.Contains(Path.GetExtension(f!.FileName)))
                    .WithMessage("Cover image must be .jpg, .jpeg, .png, or .webp.")
                .Must(f => AllowedImageContentTypes.Contains(f!.ContentType))
                    .WithMessage("Cover image content type is not supported.")
                .Must(ImageFileSignature.MatchesDeclaredExtension)
                    .WithMessage("Cover image content does not match its file extension.");

            RuleFor(x => x.PdfFile)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("PDF file is required.")
                .Must(f => f!.Length > 0 && f.Length <= MaxPdfBytes)
                    .WithMessage("PDF must be between 1 byte and 100 MB.")
                .Must(f => Path.GetExtension(f!.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                    .WithMessage("Book content file must be a .pdf.")
                .Must(f => AllowedPdfContentTypes.Contains(f!.ContentType))
                    .WithMessage("PDF content type is not supported.")
                .Must(DocumentFileSignature.MatchesDeclaredExtension)
                    .WithMessage("PDF content does not match its file extension.");

            RuleFor(x => x.WordFile)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("Word document is required.")
                .Must(f => f!.Length > 0 && f.Length <= MaxWordBytes)
                    .WithMessage("Word document must be between 1 byte and 50 MB.")
                .Must(f => AllowedWordExtensions.Contains(Path.GetExtension(f!.FileName)))
                    .WithMessage("Word document must be .doc or .docx.")
                .Must(f => AllowedWordContentTypes.Contains(f!.ContentType))
                    .WithMessage("Word document content type is not supported.")
                .Must(DocumentFileSignature.MatchesDeclaredExtension)
                    .WithMessage("Word document content does not match its file extension.");
        }
    }
}
