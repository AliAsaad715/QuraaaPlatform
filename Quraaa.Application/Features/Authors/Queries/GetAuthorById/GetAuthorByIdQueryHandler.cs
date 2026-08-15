using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authors.Common;
using Quraaa.Application.Features.Authors.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Authors.Queries.GetAuthorById
{
    public class GetAuthorByIdQueryHandler
        : BaseApplicationService<GetAuthorByIdQueryHandler>,
          IRequestHandler<GetAuthorByIdQuery, AppResult<AuthorDetailsResponse>>
    {
        private readonly IAuthorRepository _authorRepository;

        public GetAuthorByIdQueryHandler(
            IAuthorRepository authorRepository,
            ILogger<GetAuthorByIdQueryHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _authorRepository = authorRepository;
        }

        public async Task<AppResult<AuthorDetailsResponse>> Handle(GetAuthorByIdQuery request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync<GetAuthorByIdQuery, AuthorDetailsResponse>(request, async () =>
            {
                var author = await _authorRepository.GetByIdAsync(request.Id, cancellationToken)
                    ?? throw new NotFoundException($"Author with ID {request.Id} was not found.");

                return new AuthorDetailsResponse(
                    author.Id,
                    author.Name,
                    author.Bio,
                    author.PhotoUrl,
                    author.BirthDate,
                    author.CreationTime,
                    author.LastModificationTime
                );
            }, "Author retrieved successfully");
        }
    }
}
