using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;

namespace MarketDashboard.Web.Auth;

public class IdentityRedirectManager(NavigationManager navigationManager)
{
    public const string StatusCookieName = "Identity.StatusMessage";

    private static readonly CookieBuilder StatusCookieBuilder = new()
    {
        SameSite = SameSiteMode.Strict,
        HttpOnly = true,
        IsEssential = true,
        MaxAge = TimeSpan.FromSeconds(5)
    };

    public void RedirectTo(string uri)
    {
        navigationManager.NavigateTo(uri, forceLoad: true);
    }

    public void RedirectTo(string uri, Dictionary<string, object?> queryParameters)
    {
        var uriWithQuery = navigationManager.GetUriWithQueryParameters(uri, queryParameters);
        navigationManager.NavigateTo(uriWithQuery, forceLoad: true);
    }

    public void RedirectToWithStatus(string uri, string message, HttpContext context)
    {
        context.Response.Cookies.Append(
            StatusCookieName,
            message,
            StatusCookieBuilder.Build(context));
        RedirectTo(uri);
    }

    public void RedirectToCurrentPage()
    {
        navigationManager.NavigateTo(navigationManager.Uri, forceLoad: true);
    }

    public void RedirectToLogin()
    {
        RedirectTo("/Account/Login");
    }
}

