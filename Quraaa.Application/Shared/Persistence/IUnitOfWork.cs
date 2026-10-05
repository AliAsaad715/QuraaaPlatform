namespace Quraaa.Application.Shared.Persistence
{
    /// <summary>
    /// Commits what the current request's repositories staged. Repositories only
    /// read and stage changes; a handler decides when its unit of work is complete.
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>
        /// Saves every staged change as one atomic write. Persistence failures are
        /// translated into application exceptions: optimistic-concurrency and
        /// race-condition failures surface as <c>ConflictException</c>, and known
        /// uniqueness violations as their documented error codes.
        /// </summary>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Runs <paramref name="operation"/> in one database transaction, committing
        /// when it completes and rolling back when it throws. Use it when several
        /// saves, including ones made by ASP.NET Core Identity, must succeed or fail
        /// together.
        /// </summary>
        Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default);
    }
}
