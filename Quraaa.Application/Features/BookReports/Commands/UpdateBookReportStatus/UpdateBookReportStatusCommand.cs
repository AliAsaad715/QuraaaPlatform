using MediatR;
using Quraaa.Application.Features.BookReports.Common;
using Quraaa.Application.Shared.Results;
using Quraaa.Domain.Reports.Enums;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.BookReports.Commands.UpdateBookReportStatus
{
    /// <summary>
    /// Administrator command: records the outcome of investigating a report.
    /// </summary>
    public record UpdateBookReportStatusCommand : IRequest<AppResult<BookReportResponse>>
    {
        [JsonIgnore]
        public Guid ReportId { get; init; }

        [JsonIgnore]
        public Guid AdminId { get; init; }

        /// <summary>InReview, Resolved, or Rejected.</summary>
        public required BookReportStatus Status { get; init; }

        /// <summary>Optional note describing what was found or decided.</summary>
        public string? ModeratorNote { get; init; }
    }
}
