using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.DomainEvents.Commands.DispatchDomainEvents;
using Quraaa.Application.Features.DomainEvents.Interfaces;

namespace Quraaa.Infrastructure.Workers
{
    /// <summary>
    /// Drains the domain event outbox: hands each stored batch of events to its
    /// MediatR handlers, one batch per scope. It runs a pass as soon as a save
    /// signals new events and otherwise scans on a short interval, which also
    /// covers retries and events stored by another replica.
    /// </summary>
    public sealed class DomainEventDispatcherService : BackgroundService
    {
        private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(5);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IDomainEventDispatchSignal _dispatchSignal;
        private readonly ILogger<DomainEventDispatcherService> _logger;

        public DomainEventDispatcherService(
            IServiceScopeFactory scopeFactory,
            IDomainEventDispatchSignal dispatchSignal,
            ILogger<DomainEventDispatcherService> logger)
        {
            _scopeFactory = scopeFactory;
            _dispatchSignal = dispatchSignal;
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
                var handledBatch = false;

                try
                {
                    // A fresh scope per batch: a failed batch's staged changes are
                    // discarded with its DbContext instead of reaching the next one.
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                    var result = await sender.Send(new DispatchDomainEventsCommand(), stoppingToken);

                    handledBatch = result.Outcome != DomainEventBatchOutcome.None;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Domain event dispatch cycle failed.");
                }

                if (handledBatch)
                {
                    // More batches may be ready: drain them before waiting. A failed
                    // batch is rescheduled, so it cannot be claimed again right away.
                    continue;
                }

                try
                {
                    await _dispatchSignal.WaitForSignalAsync(ScanInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}
