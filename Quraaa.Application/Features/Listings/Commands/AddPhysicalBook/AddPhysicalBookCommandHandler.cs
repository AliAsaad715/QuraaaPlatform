using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authors.Interfaces;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Catalog;
using Quraaa.Domain.Marketplace;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Listings.Commands.AddPhysicalBook
{
    public class AddPhysicalBookCommandHandler
        : BaseApplicationService<AddPhysicalBookCommandHandler>,
          IRequestHandler<AddPhysicalBookCommand, AppResult<AddPhysicalBookResponse>>
    {
        private readonly ILibraryRepository _libraryRepository;
        private readonly IBookRepository _bookRepository;
        private readonly IAuthorRepository _authorRepository;
        private readonly IListingRepository _listingRepository;
        private readonly IBookMetadataService _bookMetadataService;

        public AddPhysicalBookCommandHandler(
            ILibraryRepository libraryRepository,
            IBookRepository bookRepository,
            IAuthorRepository authorRepository,
            IListingRepository listingRepository,
            IBookMetadataService bookMetadataService,
            ILogger<AddPhysicalBookCommandHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _libraryRepository = libraryRepository;
            _bookRepository = bookRepository;
            _authorRepository = authorRepository;
            _listingRepository = listingRepository;
            _bookMetadataService = bookMetadataService;
        }

        public async Task<AppResult<AddPhysicalBookResponse>> Handle(
            AddPhysicalBookCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                // ── Step 1: Resolve the caller's library ──────────────────────
                var library = await _libraryRepository
                    .GetApprovedByUserIdAsync(request.RequestingUserId, cancellationToken);

                if (library is null)
                    throw new NotFoundException("Library was not found.");

                // ── Step 2: Resolve (or create) the BookAggregate ─────────────
                var book = await ResolveByIsbnAsync(request, cancellationToken);

                // ── Step 3: Guard — listing must not already exist ─────────────
                if (await _listingRepository.ExistsByLibraryAndBookAsync(
                        library.Id, book.Id, cancellationToken))
                    throw new ConflictException("This book is already listed in your library.");

                // ── Step 4: Create listing (PendingReview — requires approval) ─
                var listing = ListingAggregate.CreateForLibrary(
                    id: Guid.NewGuid(),
                    bookId: book.Id,
                    libraryId: library.Id,
                    price: request.Price,
                    condition: request.Condition,
                    stock: request.Quantity
                );

                await _listingRepository.AddAsync(listing, cancellationToken);
                await _listingRepository.SaveChangesAsync(cancellationToken);

                return new AddPhysicalBookResponse(listing.Id);

            }, "Physical book added to library successfully.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // ISBN resolution path
        //   1. DB by ISBN
        //   2. Google Books API  →  DB by title/author/language  →  create
        // ─────────────────────────────────────────────────────────────────────
        private async Task<BookAggregate> ResolveByIsbnAsync(
            AddPhysicalBookCommand request,
            CancellationToken cancellationToken)
        {
            var cleanIsbn = request.Isbn!.Replace("-", "").Trim();

            // 1. DB lookup by ISBN
            var book = await _bookRepository.FindByIsbnAsync(cleanIsbn, cancellationToken);
            if (book is not null)
                return book;

            // 2. Google Books API
            var metadata = await _bookMetadataService
                .GetBookByIsbnAsync(cleanIsbn, cancellationToken);

            if (metadata is null)
            {
                throw new NotFoundException("Book not found.");
            }

            var language = LanguageCodeMapper.Parse(metadata.Language);

            // The same book might already exist in the DB without an ISBN
            book = await _bookRepository.FindByTitleAuthorLanguageAsync(
                metadata.Title,
                metadata.Authors,
                language,
                cancellationToken);

            if (book is null)
            {
                var author = await _authorRepository.FindOrCreateByNameAsync(metadata.Authors, cancellationToken);

                book = new BookAggregate(
                    id: Guid.NewGuid(),
                    title: metadata.Title,
                    authorId: author.Id,
                    description: metadata.Description,
                    coverImageUrl: metadata.ThumbnailUrl,
                    language: language,
                    isbn: cleanIsbn);

                await _bookRepository.AddAsync(book, cancellationToken);
            }
            return book;
        }
    }
}