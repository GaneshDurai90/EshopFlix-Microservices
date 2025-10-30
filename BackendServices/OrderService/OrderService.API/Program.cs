using MassTransit;
using MassTransit.Definition;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
//using OrderService.API.Consumers;
using OrderService.Infrastructure;
//using OrderStateMachine.Database;
//using OrderStateMachine.Database.Entities;
//using OrderStateMachine.StateMachine;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
ServiceRegistration.RegisterServices(builder.Services, builder.Configuration);

// Add services to the container.
builder.Services.TryAddSingleton(KebabCaseEndpointNameFormatter.Instance);

builder.Services.AddMassTransit(config =>
{
    ////State Machine Consumer
    //config.AddConsumer<OrderStartConsumer>();
    //config.AddConsumer<OrderAcceptedConsumer>();
    //config.AddConsumer<OrderCancelledConsumer>();

    ////State Machine
    //config.AddSagaStateMachine<OrderMachine, OrderState>()
    //.EntityFrameworkRepository(r =>
    //{
    //    r.ConcurrencyMode = ConcurrencyMode.Pessimistic;
    //    r.AddDbContext<DbContext, OrderStateDbContext>((provider, options) =>
    //    {
    //        options.UseSqlServer(builder.Configuration.GetConnectionString("DbConnection"));
    //    });
    //});

    ////Azure Service Bus
    //config.UsingAzureServiceBus((ctx, cfg) =>
    //{
    //    var connectionString = builder.Configuration["ServiceBus:ConnectionString"];
    //    cfg.Host(connectionString);

    //    // Configure endpoints for other consumers or sagas
    //    cfg.ConfigureEndpoints(ctx);
    //});
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
