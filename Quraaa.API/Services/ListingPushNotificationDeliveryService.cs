using MediatR;
using Quraaa.Application.Features.Notifications.Commands.DispatchListingPushNotifications;

namespace Quraaa.API.Services;

public sealed class ListingPushNotificationDeliveryService : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(30);
    private const int BatchSize = 20;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ListingPushNotificationDeliveryService> _logger;

    public ListingPushNotificationDeliveryService(
        IServiceScopeFactory scopeFactory,
        ILogger<ListingPushNotificationDeliveryService> logger)
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
                    new DispatchListingPushNotificationsCommand(BatchSize),
                    stoppingToken);

                if (result.ClaimedCount > 0)
                {
                    _logger.LogInformation(
                        "Listing push notification delivery: {ClaimedCount} claimed, " +
                        "{CompletedCount} completed, {RetryScheduledCount} retry scheduled, " +
                        "{AbandonedCount} abandoned.",
                        result.ClaimedCount,
                        result.CompletedCount,
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
                    "Listing push notification delivery cycle failed.");
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
