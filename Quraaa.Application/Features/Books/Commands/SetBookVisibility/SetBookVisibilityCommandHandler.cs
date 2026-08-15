using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.BookReports.Interfaces;
using Quraaa.Application.Features.Books.Common;
using Quraaa.Application.Features.Books.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Books.Commands.SetBookVisibility
{
    public class SetBookVisibilityCommandHandler
        : BaseApplicationService<SetBookVisibilityCommandHandler>,
          IRequestHandler<SetBookVisibilityCommand, AppResult<BookModerationResponse>>
    {
        private readonly IBookVersionRepository _bookVersionRepository;
        private readonly IBookReportRepository _bookReportRepository;

        public SetBookVisibilityCommandHandler(
            IBookVersionRepository bookVersionRepository,
            IBookReportRepository bookReportRepository,
            ILogger<SetBookVisibilityCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _bookVersionRepository = bookVersionRepository;
            _bookReportRepository = bookReportRepository;
        }

        public async Task<AppResult<BookModerationResponse>> Handle(
            SetBookVisibilityCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<SetBookVisibilityCommand, BookModerationResponse>(request, async () =>
            {
                var book = await _bookVersionRepository.GetBookForUpdateAsync(
                    request.BookId,
                    cancellationToken);

                if (book is null)
                {
                    throw new NotFoundException("Book was not found.");
                }

                if (request.Hidden)
                {
                    book.HideForReview(request.ModerationNote, request.AdminId);
                }
                else
                {
                    book.Restore(request.ModerationNote, request.AdminId);
                }

                await _bookVersionRepository.SaveChangesAsync(cancellationToken);

                Logger.LogInformation(
                    "Admin {AdminId} set book {BookId} moderation status to {ModerationStatus}.",
                    request.AdminId,
                    book.Id,
                    book.ModerationStatus);

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
            }, "Book visibility updated successfully");
        }
    }
}
