using eShopFlix.Web.HttpClients;
using eShopFlix.Web.Filters;
using eShopFlix.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<CartInfoFilter>();
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<JwtForwardingHandler>();
builder.Services.AddScoped<IAuthTicketService, AuthTicketService>();
builder.Services.AddScoped<CartInfoFilter>();

// Central API Gateway base address
var apiGatewayBase = builder.Configuration["ApigatewayAddress"] ?? "https://localhost:7269";

// Add Catalog image base URL mapping (to fully-qualify relative ImageUrl from backend)
// Expecting Catalog:ImageBaseUrl in appsettings to point to CatalogService public base (https://localhost:7159)

// Register typed HttpClients for backend services (through the API Gateway)
builder.Services.AddHttpClient<CatalogServiceClient>(client =>
{
    client.BaseAddress = new Uri(apiGatewayBase);
}).AddHttpMessageHandler<JwtForwardingHandler>();

builder.Services.AddHttpClient<AuthServiceClient>(client =>
{
    client.BaseAddress = new Uri(apiGatewayBase);
});

builder.Services.AddHttpClient<CartServiceClient>(client =>
{
    client.BaseAddress = new Uri(apiGatewayBase);
}).AddHttpMessageHandler<JwtForwardingHandler>();

builder.Services.AddHttpClient<PaymentServiceClient>(client =>
{
    client.BaseAddress = new Uri(apiGatewayBase);
}).AddHttpMessageHandler<JwtForwardingHandler>();

builder.Services.AddHttpClient<StockServiceClient>(client =>
{
    client.BaseAddress = new Uri(apiGatewayBase);
}).AddHttpMessageHandler<JwtForwardingHandler>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "eShopFlixCookie";
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
  );

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();