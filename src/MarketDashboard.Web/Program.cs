using MarketDashboard.Infrastructure;
using MarketDashboard.Infrastructure.Data;
using MarketDashboard.Web.Components;
using MarketDashboard.Web.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using MarketDashboard.Infrastructure.Workers;
using MarketDashboard.Web.Hubs;
using MarketDashboard.Web.Middleware;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Infrastructure: DbContext + AddIdentityCore (registered inside AddInfrastructure)
builder.Services.AddInfrastructure(builder.Configuration);

// 2. Authentication cookie schemes — completes AddIdentityCore chain
// DO NOT call AddIdentity<> here — it's already called as AddIdentityCore
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

// 3. Authorization
builder.Services.AddAuthorization();

// 4. Blazor auth state
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider,
    RevalidatingIdentityAuthenticationStateProvider<ApplicationUser>>();

// 5. Blazor Server
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSignalR();

builder.Services.AddSingleton<IPriceUpdateBroadcaster,
    SignalRPriceUpdateBroadcaster>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<DatabaseSeeder>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "MarketDashboard API",
        Version = "v1",
        Description = "Financial Market Data Dashboard REST API. Use /Account/Login to authenticate, then test endpoints with cookie session."
    });
});

var app = builder.Build();

app.UseGlobalExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json",
            "MarketDashboard API v1");
        c.RoutePrefix = "api-docs";
    });
}


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapHub<MarketHub>("/hubs/market");

// Identity Razor Pages endpoints (for Login/Register/Logout)
app.MapAdditionalIdentityEndpoints();
app.MapControllers();

// Seed Admin role
using (var scope = app.Services.CreateScope())
{
    var adminService = scope.ServiceProvider
        .GetRequiredService<MarketDashboard.Infrastructure.Services.AdminService>();
    await adminService.EnsureAdminRoleExistsAsync(
        CancellationToken.None);
}

// Database Seeding
using (var seedScope = app.Services.CreateScope())
{
    var logger = seedScope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Running database seed...");
    var seeder = seedScope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync(CancellationToken.None);
    logger.LogInformation("Database seed complete");
}

app.Run();
