using MediatR;
using Quraaa.Application.Features.BookReports.Commands.DispatchBookModerationNotifications;

namespace Quraaa.API.Services;

/// <summary>
/// Delivers queued book moderation notices to libraries and administrators.
/// Mirrors <see cref="LibraryApprovalNotificationDeliveryService"/>.
/// </summary>
public sealed class BookModerationNotificationDeliveryService : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(30);
    private const int BatchSize = 20;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookModerationNotificationDeliveryService> _logger;

    public BookModerationNotificationDeliveryService(
        IServiceScopeFactory scopeFactory,
        ILogger<BookModerationNotificationDeliveryService> logger)
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
                    new DispatchBookModerationNotificationsCommand(BatchSize),
                    stoppingToken);

                if (result.ClaimedCount > 0)
                {
                    _logger.LogInformation(
                        "Book moderation notification delivery: {ClaimedCount} claimed, " +
                        "{SentCount} sent, {RetryScheduledCount} retry scheduled, {AbandonedCount} abandoned.",
                        result.ClaimedCount,
                        result.SentCount,
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
                    "Book moderation notification delivery cycle failed.");
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
