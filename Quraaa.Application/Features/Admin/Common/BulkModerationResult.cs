namespace Quraaa.Application.Features.Admin.Common
{
    /// <summary>
    /// What happened to one record in a bulk operation. Bulk actions are
    /// deliberately partial: a record that cannot be touched is reported and
    /// skipped rather than failing the whole request.
    /// </summary>
    /// <param name="Id">The record.</param>
    /// <param name="Succeeded">Whether the action was applied.</param>
    /// <param name="Reason">Why it was skipped, when it was.</param>
    /// <param name="Blockers">
    /// For a blocked permanent delete, what still references the record.
    /// </param>
    public record BulkModerationOutcome(
        Guid Id,
        bool Succeeded,
        string? Reason = null,
        IReadOnlyCollection<EntityDeletionBlocker>? Blockers = null);

    /// <summary>The outcome of a bulk moderation action.</summary>
    public record BulkModerationResult(
        int SucceededCount,
        int SkippedCount,
        IReadOnlyCollection<BulkModerationOutcome> Results)
    {
        public static BulkModerationResult From(IReadOnlyCollection<BulkModerationOutcome> outcomes) =>
            new(
                outcomes.Count(outcome => outcome.Succeeded),
                outcomes.Count(outcome => !outcome.Succeeded),
                outcomes);
    }
}
