using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Admin.Common;
using Quraaa.Application.Features.Admin.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.Admin.Commands.SetLibraryActivation
{
    public class SetLibraryActivationCommandHandler
        : BaseApplicationService<SetLibraryActivationCommandHandler>,
          IRequestHandler<SetLibraryActivationCommand, AppResult<BulkModerationResult>>
    {
        private readonly IAdminModerationRepository _moderationRepository;

        public SetLibraryActivationCommandHandler(
            IAdminModerationRepository moderationRepository,
            ILogger<SetLibraryActivationCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _moderationRepository = moderationRepository;
        }

        public async Task<AppResult<BulkModerationResult>> Handle(
            SetLibraryActivationCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<SetLibraryActivationCommand, BulkModerationResult>(request, async () =>
            {
                var ids = request.Ids.Distinct().ToArray();
                var records = await _moderationRepository.GetLibrariesByIdsAsync(ids, cancellationToken);
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
                    "Admin {AdminId} set activation ({Deactivate}) on {SucceededCount} of {RequestedCount} libraries.",
                    request.AdminId,
                    request.Deactivate,
                    outcomes.Count(outcome => outcome.Succeeded),
                    ids.Length);

                return BulkModerationResult.From(outcomes);
            }, "Libraries updated successfully");
        }
    }
}
