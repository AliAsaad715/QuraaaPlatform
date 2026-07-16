using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.Application.Features.Carts.Commands.ProcessStripeWebhook;

namespace Quraaa.API.Controllers
{
    public class PaymentsController : ApiClientController
    {
        [AllowAnonymous]
        [HttpPost("stripe/webhook")]
        public async Task<IActionResult> StripeWebhook()
        {
            using var reader = new StreamReader(Request.Body);
            var payload = await reader.ReadToEndAsync();
            var signature = Request.Headers["Stripe-Signature"].ToString();

            var result = await Mediator.Send(new ProcessStripeWebhookCommand(payload, signature));
            return HandleResult(result);
        }
    }
}
