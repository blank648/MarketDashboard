using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MarketDashboard.Web.Auth;

public class RevalidatingIdentityAuthenticationStateProvider<TUser>(
    ILoggerFactory loggerFactory,
    IServiceScopeFactory scopeFactory)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory)
    where TUser : class
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(30);

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState,
        CancellationToken cancellationToken)
    {
        IServiceScope? scope = null;

        try
        {
            scope = _scopeFactory.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();
            var principal = authenticationState.User;
            var userId = userManager.GetUserId(principal);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return false;
            }

            var user = await userManager.FindByIdAsync(userId);

            if (user is null)
            {
                return false;
            }

            var validator = scope.ServiceProvider.GetService<IUserClaimsPrincipalFactory<TUser>>();

            if (validator is not null)
            {
                var stamp = await userManager.GetSecurityStampAsync(user);
                return !string.IsNullOrEmpty(stamp);
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            scope?.Dispose();
        }
    }
}


