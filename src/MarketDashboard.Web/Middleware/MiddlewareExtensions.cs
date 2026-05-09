namespace MarketDashboard.Web.Middleware;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(
        this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionHandlingMiddleware>();
}
