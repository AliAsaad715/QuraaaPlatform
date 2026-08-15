using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.BookReports.Common;
using Quraaa.Application.Features.BookReports.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Reports;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.BookReports.Commands.CreateBookReport
{
    public class CreateBookReportCommandHandler
        : BaseApplicationService<CreateBookReportCommandHandler>,
          IRequestHandler<CreateBookReportCommand, AppResult<BookReportResponse>>
    {
        private readonly IBookReportRepository _bookReportRepository;
        private readonly IUserRepository _userRepository;

        public CreateBookReportCommandHandler(
            IBookReportRepository bookReportRepository,
            IUserRepository userRepository,
            ILogger<CreateBookReportCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _bookReportRepository = bookReportRepository;
            _userRepository = userRepository;
        }

        public async Task<AppResult<BookReportResponse>> Handle(
            CreateBookReportCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<CreateBookReportCommand, BookReportResponse>(request, async () =>
            {
                var user = await _userRepository.GetUserByIdAsync(request.UserId);
                if (user is null)
                {
                    throw new NotFoundException("User was not found.");
                }

                if (!await _bookReportRepository.BookExistsAsync(request.BookId, cancellationToken))
                {
                    throw new NotFoundException("Book was not found.");
                }

                if (await _bookReportRepository.ExistsForUserAndBookAsync(
                        request.UserId,
                        request.BookId,
                        cancellationToken))
                {
                    throw new ConflictException(BookReportErrorCodes.DuplicateBookReportMessage);
                }

                var report = BookReportAggregate.Create(
                    request.UserId,
                    request.BookId,
                    request.Reason,
                    request.Details);

                await _bookReportRepository.AddAsync(report, cancellationToken);

                try
                {
                    await _bookReportRepository.SaveChangesAsync(cancellationToken);
                }
                catch (ApplicationBusinessException exception) when (
                    string.Equals(
                        exception.Message,
                        BookReportErrorCodes.DuplicateBookReport,
                        StringComparison.Ordinal))
                {
                    // Two submissions raced past the pre-check; the unique index
                    // settled it, so report the same conflict either way.
                    throw new ConflictException(BookReportErrorCodes.DuplicateBookReportMessage);
                }

                Logger.LogInformation(
                    "User {UserId} reported book {BookId} for {Reason} (report {ReportId}).",
                    request.UserId,
                    request.BookId,
                    request.Reason,
                    report.Id);

                return await _bookReportRepository.GetResponseByIdAsync(report.Id, cancellationToken)
                    ?? throw new NotFoundException("The report was not found after creation.");
            }, "Book report submitted successfully");
        }
    }
}
