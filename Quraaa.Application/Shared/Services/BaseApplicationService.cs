using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Shared.Services
{
    public abstract class BaseApplicationService<TService>
    {
        protected readonly ILogger<TService> Logger;
        private readonly IServiceProvider _serviceProvider; // <--- NEW

        protected BaseApplicationService(ILogger<TService> logger, IServiceProvider serviceProvider)
        {
            Logger = logger;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Automatically finds the validator for TRequest and executes the logic.
        /// </summary>
        protected async Task<AppResult> ExecuteAsync<TRequest>(
     TRequest request,
     Func<Task> businessLogic,
     string successMessage = "Operation successful")
        {
            var validator = _serviceProvider.GetService<IValidator<TRequest>>();

            if (validator != null)
            {
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    Logger.LogWarning("Validation failed for {RequestType}: {Errors}",
                        typeof(TRequest).Name, validationResult.Errors);

                    return Result.ValidationFailed(validationResult.Errors);
                }
            }
            else
            {
                Logger.LogWarning("No validator found for {Type}", typeof(TRequest).Name);
            }

            try
            {
                await businessLogic();

                Logger.LogInformation("{SuccessMessage} | User: {User}",
                    successMessage, SafeGetCurrentUserIdForLog());

                return Result.Success();
            }
            catch (NotFoundException ex)
            {
                Logger.LogWarning("Resource not found: {Message}", ex.Message);
                return Result.NotFound(ex.Message);
            }
            catch (DomainException ex)
            {
                Logger.LogWarning(ex, "Domain rule violation in {RequestType}", typeof(TRequest).Name);
                return Result.DomainError(ex.Message);
            }
            catch (ApplicationBusinessException ex)
            {
                Logger.LogWarning(ex, "Application business rule violation: {Message}", ex.Message);
                return Result.DomainError(ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                Logger.LogWarning("Security violation by User {User}", SafeGetCurrentUserIdForLog());
                return Result.Forbidden();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unhandled error in {RequestType}", typeof(TRequest).Name);
                throw; // Let global middleware handle 500
            }
        }

        protected async Task<AppResult<TData>> ExecuteAsync<TRequest, TData>(
     TRequest request,
     Func<Task<TData>> businessLogic,
     string successMessage = "Operation successful")
        {
            var validator = _serviceProvider.GetService<IValidator<TRequest>>();

            if (validator != null)
            {
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    Logger.LogWarning("Validation failed for {RequestType}: {Errors}",
                        typeof(TRequest).Name, validationResult.Errors);

                    return Result.ValidationFailed(validationResult.Errors);
                }
            }
            else
            {
                Logger.LogWarning("No validator found for {Type}", typeof(TRequest).Name);
            }

            try
            {
                var data = await businessLogic();

                Logger.LogInformation("{SuccessMessage} | User: {User}",
                    successMessage, SafeGetCurrentUserIdForLog());

                return data;
            }
            catch (NotFoundException ex)
            {
                Logger.LogWarning("Resource not found: {Message}", ex.Message);
                return Result.NotFound();
            }
            catch (DomainException ex)
            {
                Logger.LogWarning(ex, "Domain rule violation in {RequestType}", typeof(TRequest).Name);
                return Result.DomainError(ex.Message);
            }
            catch (ApplicationBusinessException ex)
            {
                Logger.LogWarning(ex, "Application business rule violation: {Message}", ex.Message);
                return Result.DomainError(ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                Logger.LogWarning("Security violation by User {User}", SafeGetCurrentUserIdForLog());
                return Result.Forbidden();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unhandled error in {RequestType}", typeof(TRequest).Name);
                throw; // Let global middleware handle 500
            }
        }
        protected async Task<AppResult<TData>> ExecuteAsync<TData>(
    Func<Task<TData>> businessLogic,
    string successMessage = "Operation successful")
        {

            try
            {
                var data = await businessLogic();

                Logger.LogInformation("{SuccessMessage} | User: {User}",
                    successMessage, SafeGetCurrentUserIdForLog());

                return data;
            }
            catch (NotFoundException ex)
            {
                Logger.LogWarning("Resource not found: {Message}", ex.Message);
                return Result.NotFound();
            }
            catch (DomainException ex)
            {
                Logger.LogWarning(ex, "Domain rule violation in Query");
                return Result.DomainError(ex.Message);
            }
            catch (ApplicationBusinessException ex)
            {
                Logger.LogWarning(ex, "Application business rule violation: {Message}", ex.Message);
                return Result.DomainError(ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                Logger.LogWarning("Security violation by User {User}", SafeGetCurrentUserIdForLog());
                return Result.Forbidden();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unhandled error in Query");
                throw; // Let global middleware handle 500
            }
        }

        protected async Task<AppResult> ExecuteAsync(
    Func<Task> businessLogic,
    string successMessage = "Operation successful")
        {

            try
            {
                await businessLogic();

                Logger.LogInformation("{SuccessMessage} | User: {User}",
                    successMessage, SafeGetCurrentUserIdForLog());
                return Result.Success();
            }
            catch (NotFoundException ex)
            {
                Logger.LogWarning("Resource not found: {Message}", ex.Message);
                return Result.NotFound();
            }
            catch (DomainException ex)
            {
                Logger.LogWarning(ex, "Domain rule violation in Query");
                return Result.DomainError(ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                Logger.LogWarning("Security violation by User {User}", SafeGetCurrentUserIdForLog());
                return Result.Forbidden();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unhandled error in Query");
                throw; // Let global middleware handle 500
            }
        }

        private string SafeGetCurrentUserIdForLog()
        {
            try
            {
                return GetCurrentUserIdLog();
            }
            catch
            {
                // Logging should never crash the request; fall back to a neutral identity.
                return "Unknown";
            }
        }

        protected virtual string GetCurrentUserIdLog() => "System";
    }
}
