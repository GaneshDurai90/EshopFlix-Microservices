using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Application.Repositories;
using OrderService.Application.Services.Abstractions;
using OrderService.Application.Services.Implementations;
using OrderService.Infrastructure.Persistence;
using OrderService.Infrastructure.Persistence.Repositories;


namespace OrderService.Infrastructure
{
    public class ServiceRegistration
    {
        public static void RegisterServices(IServiceCollection services, IConfiguration configuration)
        {
            //DB Context
            services.AddDbContext<OrderServiceDbContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DbConnection"));
            });
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IOrderAppService, OrderAppService>();
        }
    }
}
