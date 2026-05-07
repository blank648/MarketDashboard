using MarketDashboard.Infrastructure;
using MarketDashboard.Infrastructure.Data;
using MarketDashboard.Web.Components;
using MarketDashboard.Web.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using MarketDashboard.Infrastructure.Workers;
using MarketDashboard.Web.Hubs;

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

var app = builder.Build();


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
