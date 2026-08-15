namespace Quraaa.Application.Features.Notifications.Interfaces;

public interface IPushDeviceRepository
{
    Task RegisterAsync(
        Guid userId,
        string token,
        CancellationToken cancellationToken = default);

    Task UnregisterAsync(
        Guid userId,
        string token,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> GetTokensByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task RemoveTokensAsync(
        Guid userId,
        IReadOnlyCollection<string> tokens,
        CancellationToken cancellationToken = default);
}
