using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.Application.Features.Payments.Commands.ProcessPaymentWebhook;
using Quraaa.Application.Features.Payments.Exceptions;

namespace Quraaa.API.Controllers
{
    public class PaymentsController : ApiClientController
    {
        [AllowAnonymous]
        [HttpPost("stripe/webhook")]
        public async Task<IActionResult> StripeWebhook()
        {
            using var reader = new StreamReader(Request.Body);
            var payload = await reader.ReadToEndAsync(HttpContext.RequestAborted);
            var signature = Request.Headers["Stripe-Signature"].ToString();

            try
            {
                var result = await Mediator.Send(
                    new ProcessPaymentWebhookCommand(payload, signature),
                    HttpContext.RequestAborted);

                return HandleResult(result);
            }
            catch (PaymentWebhookVerificationException exception)
            {
                return BadRequest(new
                {
                    type = "InvalidWebhookSignature",
                    title = "Invalid Stripe Webhook",
                    detail = exception.Message
                });
            }
        }
    }
}
