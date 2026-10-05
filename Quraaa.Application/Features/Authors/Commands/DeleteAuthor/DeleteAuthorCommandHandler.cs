using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authors.Interfaces;
using Quraaa.Application.Shared.Persistence;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Authors.Commands.DeleteAuthor
{
    public class DeleteAuthorCommandHandler
        : BaseApplicationService<DeleteAuthorCommandHandler>,
          IRequestHandler<DeleteAuthorCommand, AppResult>
    {
        private readonly IAuthorRepository _authorRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteAuthorCommandHandler(
            IAuthorRepository authorRepository,
            IUnitOfWork unitOfWork,
            ILogger<DeleteAuthorCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _authorRepository = authorRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<AppResult> Handle(DeleteAuthorCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var author = await _authorRepository.GetByIdAsync(request.Id, cancellationToken)
                    ?? throw new NotFoundException($"Author with ID {request.Id} was not found.");

                await _authorRepository.RemoveAsync(author, cancellationToken);

                // A book still referencing the author fails the save with a
                // ConflictException, and the author stays tracked as unchanged.
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }, "Author deleted successfully");
        }
    }
}
