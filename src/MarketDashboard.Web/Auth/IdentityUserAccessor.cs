using MarketDashboard.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace MarketDashboard.Web.Auth;

public class IdentityUserAccessor(
    UserManager<ApplicationUser> userManager,
    IdentityRedirectManager redirectManager)
{
    public async Task<ApplicationUser> GetRequiredUserAsync(HttpContext context)
    {
        var user = await userManager.GetUserAsync(context.User);

        if (user is null)
        {
            redirectManager.RedirectToWithStatus(
                "/Account/InvalidUser",
                "Error: Unable to load user.",
                context);
        }

        return user!;
    }
}

