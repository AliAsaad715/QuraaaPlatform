using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Admin.Common;
using Quraaa.Application.Features.Admin.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.User;

namespace Quraaa.Application.Features.Admin.Commands.DeleteUsers
{
    public class DeleteUsersCommandHandler
        : BaseApplicationService<DeleteUsersCommandHandler>,
          IRequestHandler<DeleteUsersCommand, AppResult<BulkModerationResult>>
    {
        private readonly IAdminModerationRepository _moderationRepository;

        public DeleteUsersCommandHandler(
            IAdminModerationRepository moderationRepository,
            ILogger<DeleteUsersCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _moderationRepository = moderationRepository;
        }

        public async Task<AppResult<BulkModerationResult>> Handle(
            DeleteUsersCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<DeleteUsersCommand, BulkModerationResult>(request, async () =>
            {
                var ids = request.Ids.Distinct().ToArray();
                var records = await _moderationRepository.GetUsersByIdsAsync(ids, cancellationToken);
                var byId = records.ToDictionary(record => record.Id);

                var blockersById = await _moderationRepository.GetUserDeletionBlockersAsync(ids, cancellationToken);

                var outcomes = new List<BulkModerationOutcome>(ids.Length);
                var removable = new List<UserAggregate>();

                foreach (var id in ids)
                {
                    if (!byId.TryGetValue(id, out var record))
                    {
                        outcomes.Add(new BulkModerationOutcome(
                            id, false, AdminModerationErrorCodes.NotFound));
                        continue;
                    }

                    if (id == request.AdminId)
                    {
                        // Locking yourself out from a bulk action is never
                        // intentional; self-service removal is a separate,
                        // confirmed flow.
                        outcomes.Add(new BulkModerationOutcome(
                            id, false, AdminModerationErrorCodes.CannotTargetSelf));
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
                    await _moderationRepository.RemoveUsersAsync(removable, cancellationToken);
                    await _moderationRepository.SaveChangesAsync(cancellationToken);
                }

                Logger.LogWarning(
                    "Admin {AdminId} permanently deleted {DeletedCount} of {RequestedCount} users.",
                    request.AdminId,
                    removable.Count,
                    ids.Length);

                return BulkModerationResult.From(outcomes);
            }, "Users deleted successfully");
        }
    }
}
