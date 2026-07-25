using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Listings.Commands.AddPhysicalBook;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Catalog;
using Quraaa.Domain.Marketplace;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Listings.Commands.AddUserPhysicalBook
{
    public class AddUserPhysicalBookCommandHandler
        : BaseApplicationService<AddUserPhysicalBookCommandHandler>,
          IRequestHandler<AddUserPhysicalBookCommand, AppResult<AddPhysicalBookResponse>>
    {
        private readonly IBookRepository _bookRepository;
        private readonly IListingRepository _listingRepository;
        private readonly IBookMetadataService _bookMetadataService;

        public AddUserPhysicalBookCommandHandler(
            IBookRepository bookRepository,
            IListingRepository listingRepository,
            IBookMetadataService bookMetadataService,
            ILogger<AddUserPhysicalBookCommandHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _bookRepository = bookRepository;
            _listingRepository = listingRepository;
            _bookMetadataService = bookMetadataService;
        }

        public async Task<AppResult<AddPhysicalBookResponse>> Handle(
            AddUserPhysicalBookCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var book = await ResolveByIsbnAsync(request, cancellationToken);

                if (await _listingRepository.ExistsByUserAndBookAsync(
                        request.RequestingUserId,
                        book.Id,
                        cancellationToken))
                {
                    throw new ConflictException("This book is already listed by you.");
                }

                var listing = ListingAggregate.CreateForUser(
                    id: Guid.NewGuid(),
                    bookId: book.Id,
                    userId: request.RequestingUserId,
                    format: ListingFormat.Physical,
                    price: request.Price,
                    condition: request.Condition);

                await _listingRepository.AddAsync(listing, cancellationToken);
                await _listingRepository.SaveChangesAsync(cancellationToken);

                return new AddPhysicalBookResponse(listing.Id);

            }, "Physical book added successfully.");
        }

        private async Task<BookAggregate> ResolveByIsbnAsync(
            AddUserPhysicalBookCommand request,
            CancellationToken cancellationToken)
        {
            var cleanIsbn = request.Isbn.Replace("-", "").Trim();

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