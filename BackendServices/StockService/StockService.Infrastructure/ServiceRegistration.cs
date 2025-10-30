using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StockService.Application.Repositories;
using StockService.Application.Services.Abstractions;
using StockService.Application.Services.Implementations;
using StockService.Infrastructure.Persistence;
using StockService.Infrastructure.Persistence.Repositories;


namespace StockService.Infrastructure
{
    public class ServiceRegistration
    {
        public static void RegisterServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<StockServiceDbContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            });
            services.AddScoped<IStockRepository, StockRepository>();
            services.AddScoped<IStockAppService, StockAppService>();
        }
    }
}
