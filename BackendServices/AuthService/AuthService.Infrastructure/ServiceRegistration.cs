using AuthService.Application.Mappers;
using AuthService.Application.Repositories;
using AuthService.Application.Services.Abstractions;
using AuthService.Application.Services.Implementations;
using AuthService.Infrastructure.Persistence;
using AuthService.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Infrastructure
{
    public class ServiceRegistration
    {
        public static void RegisterServices(IServiceCollection services,IConfiguration configuration)
        {
            // Register application services
            services.AddScoped<IUserAppService, UserAppService>();

            // Register repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

            // Register AutoMapper
            services.AddAutoMapper(cfg => cfg.AddProfile<Authmapper>());

            // Register DbContext
            services.AddDbContext<AuthServiceDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DbConnection")));
       
        }
    }
}
