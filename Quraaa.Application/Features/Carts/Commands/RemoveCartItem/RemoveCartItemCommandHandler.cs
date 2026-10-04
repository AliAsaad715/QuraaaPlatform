using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Carts.Common;
using Quraaa.Application.Features.Carts.Interfaces;
using Quraaa.Application.Shared.Persistence;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Carts.Commands.RemoveCartItem
{
    public class RemoveCartItemCommandHandler : BaseApplicationService<RemoveCartItemCommandHandler>, IRequestHandler<RemoveCartItemCommand, AppResult<CartResponse>>
    {
        private readonly ICartRepository _cartRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RemoveCartItemCommandHandler(
            ICartRepository cartRepository,
            IUnitOfWork unitOfWork,
            ILogger<RemoveCartItemCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _cartRepository = cartRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<AppResult<CartResponse>> Handle(RemoveCartItemCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var cart = await _cartRepository.GetOpenCartByUserIdAsync(request.UserId, cancellationToken)
                    ?? throw new NotFoundException("Cart not found.");

                cart.RemoveItem(request.ListingId);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return CartResponse.FromCart(cart);
            }, "Cart item removed successfully");
        }
    }
}
