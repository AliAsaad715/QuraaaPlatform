using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.BookReports.Common;
using Quraaa.Application.Features.BookReports.Interfaces;
using Quraaa.Application.Features.BookReports.Services;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.BookReports.Commands.UpdateBookReportStatus
{
    public class UpdateBookReportStatusCommandHandler
        : BaseApplicationService<UpdateBookReportStatusCommandHandler>,
          IRequestHandler<UpdateBookReportStatusCommand, AppResult<BookReportResponse>>
    {
        private readonly IBookReportRepository _bookReportRepository;
        private readonly BookReportEscalationService _escalationService;

        public UpdateBookReportStatusCommandHandler(
            IBookReportRepository bookReportRepository,
            BookReportEscalationService escalationService,
            ILogger<UpdateBookReportStatusCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _bookReportRepository = bookReportRepository;
            _escalationService = escalationService;
        }

        public async Task<AppResult<BookReportResponse>> Handle(
            UpdateBookReportStatusCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<UpdateBookReportStatusCommand, BookReportResponse>(request, async () =>
            {
                var report = await _bookReportRepository.GetByIdAsync(
                    request.ReportId,
                    cancellationToken);

                if (report is null)
                {
                    throw new NotFoundException("Book report was not found.");
                }

                var previousStatus = report.Status;

                // Already-closed reports raise ConflictException (409) from the
                // aggregate rather than silently overwriting a decision.
                report.Review(request.Status, request.AdminId, request.ModeratorNote);

                // Saved first so the recount below sees this decision, then the
                // book state is re-derived and committed in a second save.
                await _bookReportRepository.SaveChangesAsync(cancellationToken);

                await _escalationService.ReevaluateAsync(
                    report.BookId,
                    request.AdminId,
                    cancellationToken);

                await _bookReportRepository.SaveChangesAsync(cancellationToken);

                Logger.LogInformation(
                    "Admin {AdminId} moved book report {ReportId} from {PreviousStatus} to {NewStatus}.",
                    request.AdminId,
                    report.Id,
                    previousStatus,
                    report.Status);

                return await _bookReportRepository.GetResponseByIdAsync(report.Id, cancellationToken)
                    ?? throw new NotFoundException("Book report was not found.");
            }, "Book report status updated successfully");
        }
    }
}
