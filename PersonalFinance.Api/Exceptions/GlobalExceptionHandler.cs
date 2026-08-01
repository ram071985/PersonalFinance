using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace PersonalFinance.Api.Exceptions;

/// <summary>
/// Converts unhandled exceptions into consistent RFC 7807 ProblemDetails responses.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = MapException(exception);

        if (statusCode >= 500)
            _logger.LogError(exception, "Unhandled exception on {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        else
            _logger.LogWarning(exception, "Request failed ({StatusCode}) on {Method} {Path}",
                statusCode, httpContext.Request.Method, httpContext.Request.Path);

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path,
            Type = $"https://httpstatuses.com/{statusCode}"
        };

        problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        // Include exception message in Development only for 500s
        if (_env.IsDevelopment() && statusCode >= 500)
            problem.Extensions["exception"] = exception.ToString();

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }

    private static (int StatusCode, string Title, string Detail) MapException(Exception exception) =>
        exception switch
        {
            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                exception.Message),

            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "Not Found",
                exception.Message),

            ArgumentException or ArgumentNullException => (
                StatusCodes.Status400BadRequest,
                "Bad Request",
                exception.Message),

            InvalidOperationException => (
                StatusCodes.Status400BadRequest,
                "Invalid Operation",
                exception.Message),

            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                "An unexpected error occurred. Please try again later.")
        };
}
