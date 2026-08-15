using Quraaa.Domain.Reports.Enums;

namespace Quraaa.Application.Features.BookReports.Common
{
    /// <summary>
    /// The predefined report reasons offered to readers. Kept in code rather
    /// than a lookup table because the values are a domain enum: adding one is
    /// a code change either way, and this keeps the list and the enum from
    /// drifting apart.
    /// </summary>
    public static class BookReportReasonCatalog
    {
        private static readonly IReadOnlyList<BookReportReasonResponse> Reasons =
        [
            new(BookReportReason.InappropriateContent,
                "Inappropriate content",
                "محتوى غير لائق",
                RequiresDetails: false),
            new(BookReportReason.IncorrectInformation,
                "Incorrect information",
                "معلومات غير صحيحة",
                RequiresDetails: false),
            new(BookReportReason.CopyrightViolation,
                "Copyright violation",
                "انتهاك حقوق النشر",
                RequiresDetails: false),
            new(BookReportReason.OffensiveLanguage,
                "Offensive language",
                "لغة مسيئة",
                RequiresDetails: false),
            new(BookReportReason.SpamOrMisleading,
                "Spam or misleading",
                "محتوى دعائي أو مضلل",
                RequiresDetails: false),
            new(BookReportReason.PoorQuality,
                "Poor quality or unreadable",
                "جودة رديئة أو غير قابل للقراءة",
                RequiresDetails: false),
            new(BookReportReason.Other,
                "Other",
                "سبب آخر",
                RequiresDetails: true),
        ];

        public static IReadOnlyList<BookReportReasonResponse> GetAll() => Reasons;

        /// <summary>
        /// Whether a reason obliges the reporter to describe the problem.
        /// </summary>
        public static bool RequiresDetails(BookReportReason reason)
        {
            return reason == BookReportReason.Other;
        }
    }
}
