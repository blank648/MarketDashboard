using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder;

public static class IdentityComponentsEndpointRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var accountGroup = endpoints.MapGroup("/Account");

        // These endpoints are needed for logout and potentially other identity actions
        // that are not handled directly in Razor components.
        accountGroup.MapPost("/Logout", async (
            HttpContext context,
            SignInManager<MarketDashboard.Infrastructure.Data.ApplicationUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.LocalRedirect("/");
        });

        return accountGroup;
    }
}
