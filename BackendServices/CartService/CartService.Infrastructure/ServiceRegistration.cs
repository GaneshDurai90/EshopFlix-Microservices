using CartService.Application.Mappers;
using CartService.Application.Repositories;
using CartService.Application.Services.Abstractions;
using CartService.Application.Services.Implementations;
using CartService.Infrastructure.Persistence.Repositories;
using CartService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CartService.Infrastructure
{
    public class ServiceRegistration
    {
         public static void RegisterServices(IServiceCollection services, IConfiguration configuration)
        {
            //DBContext
            string ConnectionString = configuration.GetConnectionString("DbConnection");
            services.AddDbContext<CartServiceDbContext>(options => options.UseSqlServer(ConnectionString));

            //Repositories
            services.AddScoped<ICartRepository, CartRepository>();
            services.AddScoped<ICartServiceDbContextProcedures, CartServiceDbContextProcedures>();
            //Services
            services.AddScoped<ICartAppService, CartAppService>();

            //AutoMapper
            services.AddAutoMapper(conf => conf.AddProfile<CartMapper>());
        }
    }
}
