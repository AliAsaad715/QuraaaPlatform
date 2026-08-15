using FluentValidation;
using IdentityServer.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Quraaa.Application.Features.Orders.Services;
using Quraaa.Application.Features.Libraries.Services;
using Quraaa.Application.Features.BookReports.Services;
using Quraaa.Application.Features.Payouts.Interfaces;
using Quraaa.Application.Features.Payouts.Services;
using Quraaa.Application.Shared.Services;
using System.Reflection;

namespace Quraaa.Application.Extensions
{
    public static class ApplicationPackagesRegisterExtensions
    {
        public static IServiceCollection AddApplicationDependencies(this IServiceCollection services)
        {
            var assembly = typeof(ApplicationPackagesRegisterExtensions).Assembly;

            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            services.AddMediatR(cfg => {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            });

            services.AddSingleton<IImageUrlFormatter, ImageUrlFormatter>();
            services.AddScoped<IPhoneService, PhoneService>();
            services.AddScoped<LibraryRegistrationSessionService>();
            services.AddScoped<IOrderCheckoutService, OrderCheckoutService>();
            services.AddScoped<
                IOrderPaymentFinalizationService,
                OrderPaymentFinalizationService>();
            services.AddScoped<
                IOrderPaymentReconciliationService,
                OrderPaymentReconciliationService>();
            services.AddScoped<BookReportEscalationService>();
            services.AddSingleton<ISellerPayoutDispatchSignal, SellerPayoutDispatchSignal>();
            services.AddScoped<LibraryStripeOnboardingService>();
            return services;
        }
    }
}
