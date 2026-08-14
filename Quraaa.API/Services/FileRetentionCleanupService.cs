using MediatR;
using Microsoft.Extensions.Options;
using Quraaa.Application.Features.Files.Commands.RunFileRetentionCleanup;
using Quraaa.Application.Shared.Files;

namespace Quraaa.API.Services
{
    /// <summary>
    /// Periodically discovers owned private files that no listing, book, or purchase
    /// references, and hard-deletes them once they've sat
    /// unreferenced past the configured grace period. See
    /// <see cref="RunFileRetentionCleanupCommandHandler"/> for the actual logic —
    /// this class is just the timer driving it, mirroring
    /// <see cref="ExpiredOrderPaymentReconciliationService"/>'s shape.
    /// </summary>
    public sealed class FileRetentionCleanupService : BackgroundService
    {
        private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<FileRetentionCleanupService> _logger;
        private readonly FileRetentionOptions _options;

        public FileRetentionCleanupService(
            IServiceScopeFactory scopeFactory,
            IOptions<FileRetentionOptions> options,
            ILogger<FileRetentionCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Stagger the first pass so a full directory walk doesn't compete with application startup.
            try
            {
                await Task.Delay(StartupDelay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            var scanInterval = TimeSpan.FromMinutes(Math.Max(1, _options.ScanIntervalMinutes));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var sender = scope.ServiceProvider.GetRequiredService<ISender>();

                    var command = new RunFileRetentionCleanupCommand(
                        _options.GraceDays,
                        _options.DiscoveryBatchSize,
                        _options.DeletionBatchSize,
                        _options.ExcludedSubFolders);

                    var result = await sender.Send(command, stoppingToken);

                    _logger.LogInformation(
                        "File retention cleanup: scanned {FilesScanned}, {NewCandidates} new candidate(s), " +
                        "{Deleted} deleted, {Reprieved} reprieved, {Failures} failure(s).",
                        result.FilesScanned,
                        result.NewCandidatesDetected,
                        result.CandidatesDeleted,
                        result.CandidatesReprieved,
                        result.DeletionFailures);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "File retention cleanup cycle failed.");
                }

                try
                {
                    await Task.Delay(scanInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}
