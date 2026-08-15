using MediatR;
using Quraaa.Application.Features.Libraries.Commands.DispatchLibraryApprovalNotifications;

namespace Quraaa.API.Services;

public sealed class LibraryApprovalNotificationDeliveryService : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(30);
    private const int BatchSize = 20;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LibraryApprovalNotificationDeliveryService> _logger;

    public LibraryApprovalNotificationDeliveryService(
        IServiceScopeFactory scopeFactory,
        ILogger<LibraryApprovalNotificationDeliveryService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartupDelay, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                var result = await sender.Send(
                    new DispatchLibraryApprovalNotificationsCommand(BatchSize),
                    stoppingToken);

                if (result.ClaimedCount > 0)
                {
                    _logger.LogInformation(
                        "Library approval notification delivery: {ClaimedCount} claimed, " +
                        "{EmailSentCount} email sent, {EmailUncertainCount} email uncertain, " +
                        "{PushSentCount} push sent, {RetryScheduledCount} retry scheduled, " +
                        "{AbandonedCount} abandoned.",
                        result.ClaimedCount,
                        result.EmailSentCount,
                        result.EmailUncertainCount,
                        result.PushSentCount,
                        result.RetryScheduledCount,
                        result.AbandonedCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Library approval notification delivery cycle failed.");
            }

            try
            {
                await Task.Delay(ScanInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
