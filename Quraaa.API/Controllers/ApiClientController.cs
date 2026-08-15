using MediatR;
using Microsoft.AspNetCore.Mvc;
using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Features.Authentication.Common;
using Quraaa.Application.Features.Payouts.Exceptions;
using Quraaa.Application.Shared.Results;
using System.Security.Claims;

namespace Quraaa.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")] // Enforces JSON for all endpoints
    // --- GLOBAL RESPONSE TYPES ---
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public abstract class ApiClientController : ControllerBase
    {
        private IMediator? _mediator;
        protected IMediator Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<IMediator>();

        protected bool TryGetCurrentUserId(out Guid userId)
        {
            var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("nameid")
                ?? User.FindFirstValue("sub");

            return Guid.TryParse(claimValue, out userId);
        }

        protected bool TryGetCurrentSessionId(out Guid sessionId)
        {
            var claimValue = User.FindFirstValue(AuthenticationClaimNames.SessionId)
                ?? User.FindFirstValue(ClaimTypes.Sid);

            return Guid.TryParse(claimValue, out sessionId);
        }

        /// <summary>
        /// Marks a response as sensitive: never stored by caches or proxies and
        /// never leaked through the Referer header.
        /// </summary>
        protected void SetNoStoreHeaders()
        {
            Response.Headers.CacheControl = "no-store";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Append("Referrer-Policy", "no-referrer");
        }

        /// <summary>
        /// Sends a request that may reach the payment provider and maps a
        /// provider failure onto the right status: a definitive rejection is a
        /// 502 (the provider refused; retrying unchanged will not help), any
        /// other failure is a retryable 503.
        /// </summary>
        protected async Task<IActionResult> SendWithPayoutGatewayMappingAsync<TResponse>(
            IRequest<AppResult<TResponse>> request,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await Mediator.Send(request, cancellationToken);

                return HandleResult(result);
            }
            catch (PayoutGatewayException exception)
            {
                return PayoutGatewayFailure(exception);
            }
        }

        /// <summary>
        /// Maps a payment-provider failure onto a response. Definitive
        /// rejections carry the provider's own explanation, because the caller
        /// (or an administrator) has to act on it rather than retry.
        /// </summary>
        protected IActionResult PayoutGatewayFailure(PayoutGatewayException exception)
        {
            if (exception.IsDefinitiveRejection)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    type = "PaymentProviderRejected",
                    title = "Payment Provider Rejected The Request",
                    detail = exception.Message
                });
            }

            Response.Headers.RetryAfter = "60";

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                type = "ServiceUnavailable",
                title = "Payment Provider Unavailable",
                detail = "The payment provider could not be reached right now. Please try again later."
            });
        }

        protected UnauthorizedObjectResult InvalidUserIdResult()
        {
            return Unauthorized(new
            {
                type = "Unauthorized",
                title = "Invalid Authentication Token",
                detail = "The authentication token does not contain a valid user id."
            });
        }

        /// <summary>
        /// Centralized Result Handler.
        /// Maps Application Results (OneOf) to HTTP Status Codes.
        /// </summary>
        protected IActionResult HandleResult(AppResult result)
        {
            return result.Match(
                // 200 OK
                success => Ok(new { message = success.Message }),

                // 400 Bad Request (Validation)
                validationFailed => BadRequest(new
                {
                    type = "ValidationFailure",
                    title = "Validation Error",
                    errors = validationFailed.Errors.Select(e => new { Field = e.PropertyName, Message = e.ErrorMessage })
                }),

                // 404 Not Found
                NotFound => StatusCode(StatusCodes.Status404NotFound, new
                {
                    type = "NotFound",
                    title = "Resource Not Found",
                    detail = "Requested resource was not found."
                }),

                // 401 Unauthorized
                unauthorized => StatusCode(StatusCodes.Status401Unauthorized, new
                {
                    type = "Unauthorized",
                    title = "Authentication Failed",
                    detail = "Authentication credentials are invalid, expired, or revoked."
                }),

                // 403 Forbidden
                forbidden => StatusCode(StatusCodes.Status403Forbidden, new
                {
                    type = "Forbidden",
                    title = "Access Denied",
                    detail = "You do not have permission to access or modify this resource."
                }),

                // 400 Bad Request (Domain Logic)
                domainError => string.Equals(domainError.Message, LibraryErrorCodes.DuplicateLibraryForUser, StringComparison.Ordinal)
                    ? Conflict(new
                    {
                        type = "Conflict",
                        title = "Conflict",
                        detail = LibraryErrorCodes.DuplicateLibraryForUserMessage
                    })
                    : string.Equals(domainError.Message, LibraryErrorCodes.DuplicateLibraryEmail, StringComparison.Ordinal)
                    ? Conflict(new
                    {
                        type = "Conflict",
                        title = "Conflict",
                        detail = LibraryErrorCodes.DuplicateLibraryEmailMessage
                    })
                    : string.Equals(domainError.Message, "DUPLICATE_APPLICATION", StringComparison.Ordinal)
                    ? Conflict(new
                    {
                        type = "Conflict",
                        title = "Conflict",
                        detail = "You have already applied for this job offer."
                    })
                    : BadRequest(new
                    {
                        type = "DomainError",
                        title = "Business Rule Violation",
                        detail = domainError.Message
                    }),

                // 409 Conflict
                conflict => StatusCode(StatusCodes.Status409Conflict, new
                {
                    type = "Conflict",
                    title = "Conflict",
                    detail = conflict.Message
                })
            );
        }

        protected IActionResult HandleResult<T>(AppResult<T> result)
        {
            return HandleResult(result, data => Ok(data));
        }

        /// <summary>
        /// Overload for callers that need a non-200 success status (e.g. 201 Created)
        /// without altering the default success mapping every other caller relies on.
        /// </summary>
        protected IActionResult HandleResult(
            AppResult result,
            Func<Success, IActionResult> onSuccess)
        {
            return result.Match(
                onSuccess,

                // 400 Bad Request (Validation)
                validationFailed => BadRequest(new
                {
                    type = "ValidationFailure",
                    title = "Validation Error",
                    errors = validationFailed.Errors.Select(e => new { Field = e.PropertyName, Message = e.ErrorMessage })
                }),

                // 404 Not Found
                NotFound => StatusCode(StatusCodes.Status404NotFound, new
                {
                    type = "NotFound",
                    title = "Resource Not Found",
                    detail = "Requested resource was not found."
                }),

                // 401 Unauthorized
                unauthorized => StatusCode(StatusCodes.Status401Unauthorized, new
                {
                    type = "Unauthorized",
                    title = "Authentication Failed",
                    detail = "Authentication credentials are invalid, expired, or revoked."
                }),

                // 403 Forbidden
                forbidden => StatusCode(StatusCodes.Status403Forbidden, new
                {
                    type = "Forbidden",
                    title = "Access Denied",
                    detail = "You do not have permission to access or modify this resource."
                }),

                // 400 Bad Request (Domain Logic)
                domainError => string.Equals(domainError.Message, LibraryErrorCodes.DuplicateLibraryForUser, StringComparison.Ordinal)
                    ? Conflict(new
                    {
                        type = "Conflict",
                        title = "Conflict",
                        detail = LibraryErrorCodes.DuplicateLibraryForUserMessage
                    })
                    : string.Equals(domainError.Message, LibraryErrorCodes.DuplicateLibraryEmail, StringComparison.Ordinal)
                    ? Conflict(new
                    {
                        type = "Conflict",
                        title = "Conflict",
                        detail = LibraryErrorCodes.DuplicateLibraryEmailMessage
                    })
                    : string.Equals(domainError.Message, "DUPLICATE_APPLICATION", StringComparison.Ordinal)
                    ? Conflict(new
                    {
                        type = "Conflict",
                        title = "Conflict",
                        detail = "You have already applied for this job offer."
                    })
                    : BadRequest(new
                    {
                        type = "DomainError",
                        title = "Business Rule Violation",
                        detail = domainError.Message
                    }),

                // 409 Conflict
                conflict => StatusCode(StatusCodes.Status409Conflict, new
                {
                    type = "Conflict",
                    title = "Conflict",
                    detail = conflict.Message
                })
            );
        }

        protected IActionResult HandleResult<T>(
            AppResult<T> result,
            Func<T, IActionResult> onSuccess)
        {
            return result.Match(
                onSuccess,

                // 400 Bad Request (Validation)
                validationFailed => BadRequest(new
                {
                    type = "ValidationFailure",
                    title = "Validation Error",
                    errors = validationFailed.Errors.Select(e => new { Field = e.PropertyName, Message = e.ErrorMessage })
                }),

                // 404 Not Found
                NotFound => StatusCode(StatusCodes.Status404NotFound, new
                {
                    type = "NotFound",
                    title = "Resource Not Found",
                    detail = "Requested resource was not found."
                }),

                // 401 Unauthorized
                unauthorized => StatusCode(StatusCodes.Status401Unauthorized, new
                {
                    type = "Unauthorized",
                    title = "Authentication Failed",
                    detail = "Authentication credentials are invalid, expired, or revoked."
                }),

                // 403 Forbidden
                forbidden => StatusCode(StatusCodes.Status403Forbidden, new
                {
                    type = "Forbidden",
                    title = "Access Denied",
                    detail = "You do not have permission to access or modify this resource."
                }),

                // 400 Bad Request (Domain Logic)
                domainError => string.Equals(domainError.Message, LibraryErrorCodes.DuplicateLibraryForUser, StringComparison.Ordinal)
                    ? Conflict(new
                    {
                        type = "Conflict",
                        title = "Conflict",
                        detail = LibraryErrorCodes.DuplicateLibraryForUserMessage
                    })
                    : string.Equals(domainError.Message, LibraryErrorCodes.DuplicateLibraryEmail, StringComparison.Ordinal)
                    ? Conflict(new
                    {
                        type = "Conflict",
                        title = "Conflict",
                        detail = LibraryErrorCodes.DuplicateLibraryEmailMessage
                    })
                    : string.Equals(domainError.Message, "DUPLICATE_APPLICATION", StringComparison.Ordinal)
                    ? Conflict(new
                    {
                        type = "Conflict",
                        title = "Conflict",
                        detail = "You have already applied for this job offer."
                    })
                    : BadRequest(new
                    {
                        type = "DomainError",
                        title = "Business Rule Violation",
                        detail = domainError.Message
                    }),

                // 409 Conflict
                Conflict => StatusCode(StatusCodes.Status409Conflict, new
                {
                    type = "Conflict",
                    title = "Conflict",
                    detail = Conflict.Message
                })
            );
        }
    }
}
