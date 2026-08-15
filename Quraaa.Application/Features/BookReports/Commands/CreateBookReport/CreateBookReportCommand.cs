using MediatR;
using Quraaa.Application.Features.BookReports.Common;
using Quraaa.Application.Shared.Results;
using Quraaa.Domain.Reports.Enums;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.BookReports.Commands.CreateBookReport
{
    public record CreateBookReportCommand : IRequest<AppResult<BookReportResponse>>
    {
        [JsonIgnore]
        public Guid UserId { get; init; }

        [JsonIgnore]
        public Guid BookId { get; init; }

        /// <summary>One of the predefined reasons (see GET book-reports/reasons).</summary>
        public required BookReportReason Reason { get; init; }

        /// <summary>
        /// Optional free-text description, required when the reason is Other.
        /// </summary>
        public string? Details { get; init; }
    }
}
