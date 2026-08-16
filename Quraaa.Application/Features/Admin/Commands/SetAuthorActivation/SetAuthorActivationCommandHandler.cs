using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Admin.Common;
using Quraaa.Application.Features.Admin.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.Admin.Commands.SetAuthorActivation
{
    public class SetAuthorActivationCommandHandler
        : BaseApplicationService<SetAuthorActivationCommandHandler>,
          IRequestHandler<SetAuthorActivationCommand, AppResult<BulkModerationResult>>
    {
        private readonly IAdminModerationRepository _moderationRepository;

        public SetAuthorActivationCommandHandler(
            IAdminModerationRepository moderationRepository,
            ILogger<SetAuthorActivationCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _moderationRepository = moderationRepository;
        }

        public async Task<AppResult<BulkModerationResult>> Handle(
            SetAuthorActivationCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<SetAuthorActivationCommand, BulkModerationResult>(request, async () =>
            {
                var ids = request.Ids.Distinct().ToArray();
                var records = await _moderationRepository.GetAuthorsByIdsAsync(ids, cancellationToken);
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
                    "Admin {AdminId} set activation ({Deactivate}) on {SucceededCount} of {RequestedCount} authors.",
                    request.AdminId,
                    request.Deactivate,
                    outcomes.Count(outcome => outcome.Succeeded),
                    ids.Length);

                return BulkModerationResult.From(outcomes);
            }, "Authors updated successfully");
        }
    }
}
