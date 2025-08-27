using DiscountServer.Handlers;
using DiscountServer.Services;

namespace DiscountServer.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDiscountServices(this IServiceCollection services)
        {
            // Register app services
            services.AddSingleton<DiscountService>(sp =>
                new DiscountService(sp.GetRequiredService<IConfiguration>()));

            services.AddSingleton<WebSocketHandler>();
            return services;
        }
    }
}
