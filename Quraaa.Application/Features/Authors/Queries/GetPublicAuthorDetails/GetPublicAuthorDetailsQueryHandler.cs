using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authors.Common;
using Quraaa.Application.Features.Authors.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Authors.Queries.GetPublicAuthorDetails
{
    public sealed class GetPublicAuthorDetailsQueryHandler
        : BaseApplicationService<GetPublicAuthorDetailsQueryHandler>,
          IRequestHandler<GetPublicAuthorDetailsQuery, AppResult<PublicAuthorDetailsResponse>>
    {
        private readonly IAuthorRepository _authorRepository;

        public GetPublicAuthorDetailsQueryHandler(
            IAuthorRepository authorRepository,
            ILogger<GetPublicAuthorDetailsQueryHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _authorRepository = authorRepository;
        }

        public async Task<AppResult<PublicAuthorDetailsResponse>> Handle(
            GetPublicAuthorDetailsQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<GetPublicAuthorDetailsQuery, PublicAuthorDetailsResponse>(
                request,
                async () =>
                {
                    var author = await _authorRepository.GetByIdAsync(
                        request.AuthorId,
                        cancellationToken);
                    if (author is null || author.IsDeleted)
                    {
                        throw new NotFoundException(
                            $"Author with ID {request.AuthorId} was not found.");
                    }

                    return new PublicAuthorDetailsResponse(
                        author.Id,
                        author.Name,
                        author.Bio,
                        author.PhotoUrl,
                        author.BirthDate);
                },
                "Public author details retrieved successfully");
        }
    }
}
