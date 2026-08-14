using MediatR;
using Quraaa.Application.Features.Payouts.Commands.ProcessPendingSellerPayout;
using Quraaa.Application.Features.Payouts.Interfaces;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.API.Services
{
    /// <summary>
    /// Drains the seller-payout transactional outbox: finds pending payouts
    /// whose next attempt is due and sends each through the payout gateway.
    /// Payout rows are staged atomically when an order is marked paid, so this
    /// service is the only place platform money actually moves to library
    /// owners.
    /// </summary>
    public sealed class SellerPayoutProcessingService : BackgroundService
    {
        private const int BatchSize = 25;
        private static readonly TimeSpan ProcessingInterval =
            TimeSpan.FromMinutes(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SellerPayoutProcessingService> _logger;

        public SellerPayoutProcessingService(
            IServiceScopeFactory scopeFactory,
            ILogger<SellerPayoutProcessingService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessBatchAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Seller payout processing cycle failed.");
                }

                try
                {
                    await Task.Delay(ProcessingInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        private async Task ProcessBatchAsync(
            CancellationToken cancellationToken)
        {
            IReadOnlyCollection<Guid> duePayoutIds;

            await using (var scanScope = _scopeFactory.CreateAsyncScope())
            {
                var sellerPayoutRepository = scanScope.ServiceProvider
                    .GetRequiredService<ISellerPayoutRepository>();

                duePayoutIds = await sellerPayoutRepository.GetDuePayoutIdsAsync(
                    DateTime.UtcNow,
                    BatchSize,
                    cancellationToken);
            }

            var paidCount = 0;

            foreach (var payoutId in duePayoutIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // A fresh DbContext per payout keeps one optimistic-concurrency
                // conflict from contaminating the rest of the bounded batch.
                await using var payoutScope = _scopeFactory.CreateAsyncScope();
                var sender = payoutScope.ServiceProvider.GetRequiredService<ISender>();

                try
                {
                    if (await sender.Send(
                        new ProcessPendingSellerPayoutCommand(payoutId),
                        cancellationToken))
                    {
                        paidCount++;
                    }
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (ConflictException exception)
                {
                    _logger.LogDebug(
                        exception,
                        "Seller payout {PayoutId} changed while it was being processed.",
                        payoutId);
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Could not process seller payout {PayoutId}.",
                        payoutId);
                }
            }

            if (paidCount > 0)
            {
                _logger.LogInformation(
                    "Paid {PaidCount} seller payout(s) from a batch of {CandidateCount}.",
                    paidCount,
                    duePayoutIds.Count);
            }
        }
    }
}
