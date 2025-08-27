using DiscountServer.Handlers;
using DiscountServer.Services;

namespace DiscountServer.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDiscountServices(this IServiceCollection services)
        {
            // Register app services
            services.AddSingleton<DiscountService>();
            services.AddSingleton<WebSocketHandler>();

            // In the future, you could register DiscountStorage here too
            // services.AddSingleton<DiscountStorage>();

            return services;
        }
    }
}
