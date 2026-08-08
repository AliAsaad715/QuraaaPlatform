using FluentValidation;

namespace Quraaa.Application.Features.Orders.Queries.GetDigitalOrderItemDownload
{
    public class GetDigitalOrderItemDownloadQueryValidator : AbstractValidator<GetDigitalOrderItemDownloadQuery>
    {
        public GetDigitalOrderItemDownloadQueryValidator()
        {
            RuleFor(x => x.BuyerUserId).NotEmpty();
            RuleFor(x => x.OrderId).NotEmpty();
            RuleFor(x => x.OrderItemId).NotEmpty();
        }
    }
}
