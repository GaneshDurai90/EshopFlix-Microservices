using MassTransit;
//using StockService.API.Consumers;
using StockService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
ServiceRegistration.RegisterServices(builder.Services, builder.Configuration);

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddMassTransit(config =>
{
    //Add Consumer
   // config.AddConsumer<StockValidateConsumer>();

    //Azure Service Bus
  //  config.UsingAzureServiceBus((ctx, cfg) =>
//    {
//        var connectionString = builder.Configuration["ServiceBus:ConnectionString"];
//        cfg.Host(connectionString);

//        // Configure endpoints for the consumers
//        cfg.ConfigureEndpoints(ctx);
//    });
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
