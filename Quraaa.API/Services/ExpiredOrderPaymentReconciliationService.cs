using MediatR;
using Quraaa.Application.Features.Orders.Commands.ReconcileExpiredOrderPayment;
using Quraaa.Application.Features.Orders.Interfaces;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.API.Services
{
    public sealed class ExpiredOrderPaymentReconciliationService : BackgroundService
    {
        private const int BatchSize = 25;
        private static readonly TimeSpan ReconciliationInterval =
            TimeSpan.FromMinutes(1);

        // This small delay lets an already-delivered Stripe event win before
        // local expiry reconciliation, without allowing a lock to live forever.
        private static readonly TimeSpan WebhookDeliveryGracePeriod =
            TimeSpan.FromMinutes(2);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ExpiredOrderPaymentReconciliationService> _logger;

        public ExpiredOrderPaymentReconciliationService(
            IServiceScopeFactory scopeFactory,
            ILogger<ExpiredOrderPaymentReconciliationService> logger)
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
                    await ReconcileBatchAsync(stoppingToken);
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
                        "Expired order payment reconciliation cycle failed.");
                }

                try
                {
                    await Task.Delay(ReconciliationInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        private async Task ReconcileBatchAsync(
            CancellationToken cancellationToken)
        {
            var cutoffUtc = DateTime.UtcNow.Subtract(WebhookDeliveryGracePeriod);
            IReadOnlyCollection<Guid> orderIds;

            await using (var candidateScope = _scopeFactory.CreateAsyncScope())
            {
                var orderRepository = candidateScope.ServiceProvider
                    .GetRequiredService<IOrderRepository>();

                orderIds = await orderRepository.GetExpiredPendingOrderIdsAsync(
                    cutoffUtc,
                    BatchSize,
                    cancellationToken);
            }

            var reconciledCount = 0;

            foreach (var orderId in orderIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // A fresh DbContext per order keeps one optimistic-concurrency
                // conflict from contaminating the rest of the bounded batch.
                await using var orderScope = _scopeFactory.CreateAsyncScope();
                var sender = orderScope.ServiceProvider.GetRequiredService<ISender>();

                try
                {
                    if (await sender.Send(
                        new ReconcileExpiredOrderPaymentCommand(
                            orderId,
                            cutoffUtc),
                        cancellationToken))
                    {
                        reconciledCount++;
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
                        "Order {OrderId} changed while its expired payment was being reconciled.",
                        orderId);
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Could not reconcile expired payment for order {OrderId}.",
                        orderId);
                }
            }

            if (reconciledCount > 0)
            {
                _logger.LogInformation(
                    "Reconciled {ReconciledCount} expired order payment(s) from a batch of {CandidateCount}.",
                    reconciledCount,
                    orderIds.Count);
            }
        }
    }
}
