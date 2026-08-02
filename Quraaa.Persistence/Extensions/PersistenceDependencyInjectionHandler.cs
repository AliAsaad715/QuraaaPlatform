using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Books.Interfaces;
using Quraaa.Application.Features.Carts.Interfaces;
using Quraaa.Application.Features.Categories.Interfaces;
using Quraaa.Application.Features.Ebooks.Interfaces;
using Quraaa.Application.Features.FavoriteBooks.Interfaces;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Features.Orders.Interfaces;
using Quraaa.Application.Features.Purchases.Interfaces;
using Quraaa.Persistence.Repositories;
using Quraaa.Persistence.Services;

namespace Quraaa.Persistence.Extensions
{
    public static class PersistenceDependencyInjectionHandler
    {
        public static IServiceCollection AddPersistenceDependencies(this IServiceCollection services, IConfiguration configuration)
        {
            var assembly = typeof(PersistenceDependencyInjectionHandler).Assembly;

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ILibraryRepository, LibraryRepository>();
            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<IAuthenticationUnitOfWork, AuthenticationUnitOfWork>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IEbookRepository, EbookRepository>();
            services.AddScoped<IFavoriteBookRepository, FavoriteBookRepository>();
            services.AddScoped<IBookPopularityRepository, BookPopularityRepository>();
            services.AddScoped<IListingRepository, ListingRepository>();
            services.AddScoped<IBookRepository, BookRepository>();
            services.AddScoped<ICartRepository, CartRepository>();
            services.AddScoped<IBookPurchaseRepository, BookPurchaseRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IPaymentEventInbox, PaymentEventInboxRepository>();
            return services;
        }
    }
}
