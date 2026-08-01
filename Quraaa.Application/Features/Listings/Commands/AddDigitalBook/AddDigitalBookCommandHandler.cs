using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Catalog;
using Quraaa.Domain.Marketplace;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Listings.Commands.AddDigitalBook
{
    public class AddDigitalBookCommandHandler
        : BaseApplicationService<AddDigitalBookCommandHandler>,
          IRequestHandler<AddDigitalBookCommand, AppResult<AddDigitalBookResponse>>
    {
        private readonly ILibraryRepository _libraryRepository;
        private readonly IBookRepository _bookRepository;
        private readonly IListingRepository _listingRepository;
        private readonly IBookMetadataService _bookMetadataService;
        private readonly ILibraryBookStorageService _libraryBookStorageService;

        public AddDigitalBookCommandHandler(
            ILibraryRepository libraryRepository,
            IBookRepository bookRepository,
            IListingRepository listingRepository,
            IBookMetadataService bookMetadataService,
            ILibraryBookStorageService libraryBookStorageService,
            ILogger<AddDigitalBookCommandHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _libraryRepository = libraryRepository;
            _bookRepository = bookRepository;
            _listingRepository = listingRepository;
            _bookMetadataService = bookMetadataService;
            _libraryBookStorageService = libraryBookStorageService;
        }

        public async Task<AppResult<AddDigitalBookResponse>> Handle(
            AddDigitalBookCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var library = await _libraryRepository.GetApprovedByUserIdAsync(
                    request.RequestingUserId,
                    cancellationToken);

                if (library is null)
                {
                    throw new NotFoundException("Library was not found.");
                }

                var book = await ResolveByIsbnAsync(request.Isbn, cancellationToken);

                if (await _listingRepository.ExistsByLibraryAndBookAsync(
                        library.Id,
                        book.Id,
                        cancellationToken))
                {
                    throw new ConflictException("This book is already listed in your library.");
                }

                var digitalAssetUrl = await _libraryBookStorageService.SaveAsync(
                    request.DigitalAsset,
                    cancellationToken);

                var listing = ListingAggregate.CreateDigitalForLibrary(
                    id: Guid.NewGuid(),
                    bookId: book.Id,
                    libraryId: library.Id,
                    price: request.Price,
                    digitalAssetUrl: digitalAssetUrl);

                await _listingRepository.AddAsync(listing, cancellationToken);
                await _listingRepository.SaveChangesAsync(cancellationToken);

                return new AddDigitalBookResponse(listing.Id);

            }, "Digital book added to library successfully.");
        }

        private async Task<BookAggregate> ResolveByIsbnAsync(
            string isbn,
            CancellationToken cancellationToken)
        {
            var cleanIsbn = isbn.Replace("-", string.Empty).Trim();

            var book = await _bookRepository.FindByIsbnAsync(cleanIsbn, cancellationToken);
            if (book is not null)
            {
                return book;
            }

            var metadata = await _bookMetadataService.GetBookByIsbnAsync(cleanIsbn, cancellationToken);
            if (metadata is null)
            {
                throw new NotFoundException("Book not found.");
            }

            book = await _bookRepository.FindByTitleAuthorLanguageAsync(
                metadata.Title,
                metadata.Authors,
                metadata.Language,
                cancellationToken);

            if (book is null)
            {
                book = new BookAggregate(
                    id: Guid.NewGuid(),
                    title: metadata.Title,
                    author: metadata.Authors,
                    description: metadata.Description,
                    coverImageUrl: metadata.ThumbnailUrl,
                    language: metadata.Language,
                    isbn: cleanIsbn);

                await _bookRepository.AddAsync(book, cancellationToken);
            }

            return book;
        }
    }
}