using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authors.Interfaces;
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
        private readonly IAuthorRepository _authorRepository;
        private readonly IListingRepository _listingRepository;
        private readonly IBookMetadataService _bookMetadataService;
        private readonly IListingImageStorageService _listingImageStorageService;

        public AddUserPhysicalBookCommandHandler(
            IBookRepository bookRepository,
            IAuthorRepository authorRepository,
            IListingRepository listingRepository,
            IBookMetadataService bookMetadataService,
            IListingImageStorageService listingImageStorageService,
            ILogger<AddUserPhysicalBookCommandHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _bookRepository = bookRepository;
            _authorRepository = authorRepository;
            _listingRepository = listingRepository;
            _bookMetadataService = bookMetadataService;
            _listingImageStorageService = listingImageStorageService;
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

                string? coverImageUrl = null;
                try
                {
                    coverImageUrl = await _listingImageStorageService.SaveCoverImageAsync(
                        request.CoverImage,
                        cancellationToken);

                    var listing = ListingAggregate.CreateForUser(
                        id: Guid.NewGuid(),
                        bookId: book.Id,
                        userId: request.RequestingUserId,
                        format: ListingFormat.Physical,
                        price: request.Price,
                        condition: request.Condition,
                        customCoverImageUrl: coverImageUrl);

                    await _listingRepository.AddAsync(listing, cancellationToken);
                    await _listingRepository.SaveChangesAsync(cancellationToken);

                    return new AddPhysicalBookResponse(listing.Id);
                }
                catch
                {
                    if (coverImageUrl is not null)
                    {
                        try
                        {
                            await _listingImageStorageService.DeleteAsync(
                                coverImageUrl,
                                CancellationToken.None);
                        }
                        catch (Exception cleanupException)
                        {
                            Logger.LogWarning(
                                cleanupException,
                                "Failed to delete an uploaded listing cover image after listing creation failed.");
                        }
                    }

                    throw;
                }

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

            var language = LanguageCodeMapper.Parse(metadata.Language);

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