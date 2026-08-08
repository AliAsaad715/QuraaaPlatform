using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Library.Enums;
using Quraaa.Domain.User.Enums;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Libraries.Commands.UpdateLibraryApprovalStatus
{
    public class UpdateLibraryApprovalStatusCommandHandler
        : BaseApplicationService<UpdateLibraryApprovalStatusCommandHandler>,
          IRequestHandler<UpdateLibraryApprovalStatusCommand, AppResult>
    {
        private readonly ILibraryRepository _libraryRepository;
        private readonly IUserRepository _userRepository;
        private readonly IIdentityService _identityService;
        private readonly IAuthenticationUnitOfWork _unitOfWork;

        public UpdateLibraryApprovalStatusCommandHandler(
            ILibraryRepository libraryRepository,
            IUserRepository userRepository,
            IIdentityService identityService,
            IAuthenticationUnitOfWork unitOfWork,
            ILogger<UpdateLibraryApprovalStatusCommandHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _libraryRepository = libraryRepository;
            _userRepository = userRepository;
            _identityService = identityService;
            _unitOfWork = unitOfWork;
        }

        public async Task<AppResult> Handle(
            UpdateLibraryApprovalStatusCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                await _unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
                {
                    var library = await _libraryRepository.GetByIdAsync(
                        request.LibraryId,
                        transactionCancellationToken);
                    if (library is null)
                    {
                        throw new NotFoundException("Library not found.");
                    }

                    if (request.Status == LibraryApprovalStatus.Approved)
                    {
                        library.Approve(request.AdminId);

                        var user = await _userRepository.GetUserByIdAsync(library.UserId);
                        if (user is null)
                        {
                            throw new NotFoundException("The library owner was not found.");
                        }

                        user.BecomeLibraryOwner(request.AdminId);
                        var roleResult = await _identityService.AddUserToRoleAsync(
                            user.Id,
                            Role.LibraryOwner.ToString());
                        if (!roleResult.Succeeded)
                        {
                            throw new InvalidOperationException(
                                "The library was not approved because its owner role could not be assigned: " +
                                string.Join("; ", roleResult.Errors));
                        }
                    }
                    else
                    {
                        library.Reject(request.AdminId);
                    }

                    await _libraryRepository.SaveChangesAsync();
                }, cancellationToken);

            }, $"Library status updated to {request.Status} successfully.");
        }
    }
}
