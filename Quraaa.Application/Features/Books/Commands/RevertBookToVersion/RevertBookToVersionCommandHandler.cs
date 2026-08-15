using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.BookReports.Interfaces;
using Quraaa.Application.Features.Books.Common;
using Quraaa.Application.Features.Books.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Catalog;
using Quraaa.Domain.Catalog.Enums;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Books.Commands.RevertBookToVersion
{
    public class RevertBookToVersionCommandHandler
        : BaseApplicationService<RevertBookToVersionCommandHandler>,
          IRequestHandler<RevertBookToVersionCommand, AppResult<BookModerationResponse>>
    {
        private readonly IBookVersionRepository _bookVersionRepository;
        private readonly IBookReportRepository _bookReportRepository;

        public RevertBookToVersionCommandHandler(
            IBookVersionRepository bookVersionRepository,
            IBookReportRepository bookReportRepository,
            ILogger<RevertBookToVersionCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _bookVersionRepository = bookVersionRepository;
            _bookReportRepository = bookReportRepository;
        }

        public async Task<AppResult<BookModerationResponse>> Handle(
            RevertBookToVersionCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<RevertBookToVersionCommand, BookModerationResponse>(request, async () =>
            {
                var book = await _bookVersionRepository.GetBookForUpdateAsync(
                    request.BookId,
                    cancellationToken);

                if (book is null)
                {
                    throw new NotFoundException("Book was not found.");
                }

                if (request.VersionNumber >= book.CurrentVersionNumber)
                {
                    throw new ApplicationBusinessException(
                        "Only an earlier version can be restored.",
                        nameof(RevertBookToVersionCommand.VersionNumber));
                }

                var target = await _bookVersionRepository.GetVersionAsync(
                    request.BookId,
                    request.VersionNumber,
                    cancellationToken);

                if (target is null)
                {
                    throw new NotFoundException("That version of the book was not found.");
                }

                var restoredFrom = target.VersionNumber;

                // Copy the old content forward instead of deleting anything: the
                // revert itself becomes the newest version and stays auditable.
                book.ApplyDetails(
                    target.Title,
                    target.AuthorId,
                    target.Description,
                    target.CoverImageUrl,
                    target.CategoryId,
                    target.Language,
                    target.Isbn,
                    request.AdminId);

                book.RecordModerationNote(request.ModerationNote, request.AdminId);

                await _bookVersionRepository.AddAsync(
                    BookVersion.Capture(
                        book,
                        BookVersionReason.Reverted,
                        request.AdminId,
                        restoredFrom),
                    cancellationToken);

                await _bookVersionRepository.SaveChangesAsync(cancellationToken);

                Logger.LogInformation(
                    "Admin {AdminId} reverted book {BookId} to version {RestoredVersion} (now version {CurrentVersion}).",
                    request.AdminId,
                    book.Id,
                    restoredFrom,
                    book.CurrentVersionNumber);

                var reporterCount = await _bookReportRepository.CountDistinctReportersAsync(
                    book.Id,
                    includingUserId: null,
                    cancellationToken);

                return new BookModerationResponse(
                    book.Id,
                    book.Title,
                    book.ModerationStatus,
                    book.CurrentVersionNumber,
                    book.HiddenAtUtc,
                    book.ModerationNote,
                    reporterCount);
            }, "Book reverted to the selected version successfully");
        }
    }
}
