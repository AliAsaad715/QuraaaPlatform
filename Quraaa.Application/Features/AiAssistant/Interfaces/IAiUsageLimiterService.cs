namespace Quraaa.Application.Features.AiAssistant.Interfaces
{
    public record AiUsageCheckResult(bool Allowed, int DailyLimit, int RequestsUsedToday);

    public interface IAiUsageLimiterService
    {
        /// <summary>
        /// Atomically increments today's counter for this user and reports
        /// whether they're still under the daily limit. Always increments,
        /// even when the result is over budget — that's what makes the
        /// rejection sticky for the rest of the day instead of flapping.
        /// </summary>
        Task<AiUsageCheckResult> TryConsumeAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}