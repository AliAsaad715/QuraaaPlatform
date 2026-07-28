using MediatR;
using Microsoft.AspNetCore.Mvc;
using Quraaa.Application.Features.Libraries.Common;
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
                success => Ok(new { message = "Operation successful." }),

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
