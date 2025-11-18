using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;
using System.Text;
using Serilog;
using OcelotApiGateway.Logging;

var builder = WebApplication.CreateBuilder(args);

// Serilog for gateway
SerilogExtensions.ConfigureSerilog(builder.Configuration);
builder.Host.UseSerilog();

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("ocelot-dev.json");
}
else
{
    builder.Configuration.AddJsonFile("ocelot.json");
}

builder.Services.AddOcelot().AddPolly();

// Register downstream resilience delegating handler via DI so we can reference it in ocelot config
builder.Services.AddTransient<OcelotApiGateway.Resilience.DownstreamResilienceHandler>();

var endPointAuthKey = builder.Configuration["Keys:EndpointAuthKey"];
builder.Services.AddAuthentication().AddJwtBearer(endPointAuthKey, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

var app = builder.Build();

app.UseSerilogRequestLogging(opts =>
{
    opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});

app.MapGet("/", () => "Gateway is running");

await app.UseOcelot();
app.Run();
