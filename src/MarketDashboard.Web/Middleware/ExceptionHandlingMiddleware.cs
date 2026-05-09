using System.Net;
using System.Text.Json;

namespace MarketDashboard.Web.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);

            if (context.Response.StatusCode == (int)HttpStatusCode.NotFound && !context.Response.HasStarted)
            {
                await HandleExceptionAsync(context, new KeyNotFoundException("The requested resource was not found."));
            }
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext context, Exception exception)
    {
        var (statusCode, error) = exception switch
        {
            ArgumentException or ArgumentNullException
                => (HttpStatusCode.BadRequest, "Bad Request"),

            KeyNotFoundException
                => (HttpStatusCode.NotFound, "Not Found"),

            InvalidOperationException ioe
                when ioe.Message.Contains("not found",
                    StringComparison.OrdinalIgnoreCase)
                => (HttpStatusCode.NotFound, "Not Found"),

            UnauthorizedAccessException
                => (HttpStatusCode.Unauthorized, "Unauthorized"),

            InvalidOperationException ioe2
                when ioe2.Message.Contains("forbidden",
                    StringComparison.OrdinalIgnoreCase)
                => (HttpStatusCode.Forbidden, "Forbidden"),

            _   => (HttpStatusCode.InternalServerError,
                    "Internal Server Error")
        };

        var isServerError = statusCode ==
            HttpStatusCode.InternalServerError;

        if (isServerError)
            _logger.LogError(exception,
                "Unhandled exception for {Method} {Path}",
                context.Request.Method,
                context.Request.Path);
        else
            _logger.LogWarning(exception,
                "Handled exception {Status} for {Method} {Path}",
                (int)statusCode,
                context.Request.Method,
                context.Request.Path);

        var response = new
        {
            status  = (int)statusCode,
            error   = error,
            message = isServerError
                ? "An unexpected error occurred."
                : exception.Message,
            traceId = context.TraceIdentifier
        };

        context.Response.StatusCode  = (int)statusCode;
        context.Response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(response,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

        await context.Response.WriteAsync(json);
    }
}
