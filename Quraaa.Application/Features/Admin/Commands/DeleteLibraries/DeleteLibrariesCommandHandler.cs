using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Admin.Common;
using Quraaa.Application.Features.Admin.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Library;

namespace Quraaa.Application.Features.Admin.Commands.DeleteLibraries
{
    public class DeleteLibrariesCommandHandler
        : BaseApplicationService<DeleteLibrariesCommandHandler>,
          IRequestHandler<DeleteLibrariesCommand, AppResult<BulkModerationResult>>
    {
        private readonly IAdminModerationRepository _moderationRepository;

        public DeleteLibrariesCommandHandler(
            IAdminModerationRepository moderationRepository,
            ILogger<DeleteLibrariesCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _moderationRepository = moderationRepository;
        }

        public async Task<AppResult<BulkModerationResult>> Handle(
            DeleteLibrariesCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<DeleteLibrariesCommand, BulkModerationResult>(request, async () =>
            {
                var ids = request.Ids.Distinct().ToArray();
                var records = await _moderationRepository.GetLibrariesByIdsAsync(ids, cancellationToken);
                var byId = records.ToDictionary(record => record.Id);

                var blockersById = await _moderationRepository.GetLibraryDeletionBlockersAsync(ids, cancellationToken);

                var outcomes = new List<BulkModerationOutcome>(ids.Length);
                var removable = new List<LibraryAggregate>();

                foreach (var id in ids)
                {
                    if (!byId.TryGetValue(id, out var record))
                    {
                        outcomes.Add(new BulkModerationOutcome(
                            id, false, AdminModerationErrorCodes.NotFound));
                        continue;
                    }

                    if (!record.IsDeleted)
                    {
                        outcomes.Add(new BulkModerationOutcome(
                            id, false, AdminModerationErrorCodes.MustBeDeactivatedFirst));
                        continue;
                    }

                    if (blockersById.TryGetValue(id, out var blockers) && blockers.Count > 0)
                    {
                        outcomes.Add(new BulkModerationOutcome(
                            id, false, AdminModerationErrorCodes.StillReferenced, blockers));
                        continue;
                    }

                    removable.Add(record);
                    outcomes.Add(new BulkModerationOutcome(id, true));
                }

                if (removable.Count > 0)
                {
                    _moderationRepository.RemoveLibraries(removable);
                    await _moderationRepository.SaveChangesAsync(cancellationToken);
                }

                Logger.LogWarning(
                    "Admin {AdminId} permanently deleted {DeletedCount} of {RequestedCount} libraries.",
                    request.AdminId,
                    removable.Count,
                    ids.Length);

                return BulkModerationResult.From(outcomes);
            }, "Libraries deleted successfully");
        }
    }
}
