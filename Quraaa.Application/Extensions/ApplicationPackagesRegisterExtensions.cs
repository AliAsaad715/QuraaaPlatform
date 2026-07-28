using FluentValidation;
using IdentityServer.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Quraaa.Application.Features.Orders.Services;
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

            services.AddScoped<IPhoneService, PhoneService>();
            services.AddScoped<IOrderCheckoutService, OrderCheckoutService>();
            return services;
        }
    }
}
