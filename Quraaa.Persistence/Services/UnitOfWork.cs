using Microsoft.EntityFrameworkCore;
using Quraaa.Application.Shared.Persistence;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Services
{
    public sealed class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _dbContext;

        public UnitOfWork(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception)
            {
                var translated = SaveChangesExceptionTranslator.Translate(_dbContext, exception);

                if (translated is null)
                {
                    return;
                }

                if (ReferenceEquals(translated, exception))
                {
                    throw;
                }

                throw translated;
            }
        }

        public async Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = await _dbContext.Database
                .BeginTransactionAsync(cancellationToken);

            try
            {
                await operation(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
        }
    }
}
