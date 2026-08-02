using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Services
{
    public sealed class AuthenticationUnitOfWork : IAuthenticationUnitOfWork
    {
        private readonly ApplicationDbContext _dbContext;

        public AuthenticationUnitOfWork(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
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
