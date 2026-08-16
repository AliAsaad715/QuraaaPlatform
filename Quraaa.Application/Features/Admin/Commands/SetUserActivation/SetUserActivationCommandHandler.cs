using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Admin.Common;
using Quraaa.Application.Features.Admin.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.User.Enums;

namespace Quraaa.Application.Features.Admin.Commands.SetUserActivation
{
    public class SetUserActivationCommandHandler
        : BaseApplicationService<SetUserActivationCommandHandler>,
          IRequestHandler<SetUserActivationCommand, AppResult<BulkModerationResult>>
    {
        private readonly IAdminModerationRepository _moderationRepository;

        public SetUserActivationCommandHandler(
            IAdminModerationRepository moderationRepository,
            ILogger<SetUserActivationCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _moderationRepository = moderationRepository;
        }

        public async Task<AppResult<BulkModerationResult>> Handle(
            SetUserActivationCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<SetUserActivationCommand, BulkModerationResult>(request, async () =>
            {
                var ids = request.Ids.Distinct().ToArray();
                var records = await _moderationRepository.GetUsersByIdsAsync(ids, cancellationToken);
                var byId = records.ToDictionary(record => record.Id);

                var outcomes = new List<BulkModerationOutcome>(ids.Length);

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

                    if (request.Deactivate && record.Role == Role.SuperAdmin)
                    {
                        var superAdminCount = await _moderationRepository
                            .CountSuperAdminsAsync(cancellationToken);

                        if (superAdminCount <= 1)
                        {
                            outcomes.Add(new BulkModerationOutcome(
                                id, false, AdminModerationErrorCodes.LastSuperAdmin));
                            continue;
                        }
                    }

                    if (request.Deactivate)
                    {
                        record.Delete(request.AdminId);
                    }
                    else
                    {
                        record.Restore(request.AdminId);
                    }

                    outcomes.Add(new BulkModerationOutcome(id, true));
                }

                await _moderationRepository.SaveChangesAsync(cancellationToken);

                Logger.LogInformation(
                    "Admin {AdminId} set activation ({Deactivate}) on {SucceededCount} of {RequestedCount} users.",
                    request.AdminId,
                    request.Deactivate,
                    outcomes.Count(outcome => outcome.Succeeded),
                    ids.Length);

                return BulkModerationResult.From(outcomes);
            }, "Users updated successfully");
        }
    }
}
