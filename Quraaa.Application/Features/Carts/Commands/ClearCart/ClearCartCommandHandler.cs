using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Carts.Common;
using Quraaa.Application.Features.Carts.Interfaces;
using Quraaa.Application.Shared.Persistence;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Carts.Commands.ClearCart
{
    public class ClearCartCommandHandler : BaseApplicationService<ClearCartCommandHandler>, IRequestHandler<ClearCartCommand, AppResult<CartResponse>>
    {
        private readonly ICartRepository _cartRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ClearCartCommandHandler(
            ICartRepository cartRepository,
            IUnitOfWork unitOfWork,
            ILogger<ClearCartCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _cartRepository = cartRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<AppResult<CartResponse>> Handle(ClearCartCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var cart = await _cartRepository.GetOpenCartByUserIdAsync(request.UserId, cancellationToken)
                    ?? throw new NotFoundException("Cart not found.");

                cart.Clear();

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return CartResponse.FromCart(cart);
            }, "Cart cleared successfully");
        }
    }
}
