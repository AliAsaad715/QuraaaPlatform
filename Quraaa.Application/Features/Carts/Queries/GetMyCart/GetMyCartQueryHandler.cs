using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Carts.Common;
using Quraaa.Application.Features.Carts.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.Carts.Queries.GetMyCart
{
    public class GetMyCartQueryHandler : BaseApplicationService<GetMyCartQueryHandler>, IRequestHandler<GetMyCartQuery, AppResult<CartResponse>>
    {
        private readonly ICartRepository _cartRepository;

        public GetMyCartQueryHandler(
            ICartRepository cartRepository,
            ILogger<GetMyCartQueryHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _cartRepository = cartRepository;
        }

        public async Task<AppResult<CartResponse>> Handle(GetMyCartQuery request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync<GetMyCartQuery, CartResponse>(request, async () =>
            {
                var cart = await _cartRepository.GetOpenCartByUserIdAsync(request.UserId, cancellationToken);
                return cart is null ? CartResponse.Empty() : CartResponse.FromCart(cart);
            }, "Cart retrieved successfully");
        }
    }
}
