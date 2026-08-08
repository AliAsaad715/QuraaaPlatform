using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Library.Enums;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Libraries.Commands.UpdateLibraryApprovalStatus
{
    public class UpdateLibraryApprovalStatusCommandHandler
        : BaseApplicationService<UpdateLibraryApprovalStatusCommandHandler>,
          IRequestHandler<UpdateLibraryApprovalStatusCommand, AppResult>
    {
        private readonly ILibraryRepository _libraryRepository;

        public UpdateLibraryApprovalStatusCommandHandler(
            ILibraryRepository libraryRepository,
            ILogger<UpdateLibraryApprovalStatusCommandHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _libraryRepository = libraryRepository;
        }

        public async Task<AppResult> Handle(
            UpdateLibraryApprovalStatusCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var library = await _libraryRepository.GetByIdAsync(request.LibraryId, cancellationToken);
                if (library is null)
                    throw new NotFoundException("Library not found.");

                if (request.Status == LibraryApprovalStatus.Approved)
                {
                    library.Approve(request.AdminId);
                }
                else if (request.Status == LibraryApprovalStatus.Rejected)
                {
                    library.Reject(request.AdminId);
                }

                await _libraryRepository.SaveChangesAsync();

            }, $"Library status updated to {request.Status} successfully.");
        }
    }
}