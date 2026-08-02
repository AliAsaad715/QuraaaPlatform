namespace Quraaa.Application.Features.Authentication.Interfaces
{
    public interface IAuthenticationUnitOfWork
    {
        Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default);
    }
}
