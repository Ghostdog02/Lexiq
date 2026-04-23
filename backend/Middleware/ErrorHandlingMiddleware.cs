using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Middleware;

/// <summary>
/// Global error handling middleware that catches exceptions and returns appropriate HTTP status codes.
/// Maps common exception types to standardized error responses.
/// MUST be registered early in the pipeline to catch errors from all downstream middleware/controllers.
/// </summary>
public class ErrorHandlingMiddleware(
    RequestDelegate next,
    ILogger<ErrorHandlingMiddleware> logger,
    IWebHostEnvironment env
)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger = logger;
    private readonly IWebHostEnvironment _env = env;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Invokes all downstream middleware + controllers
            // If any of them throw, exception bubbles back up to the catch blocks below
            await _next(context);
        }

        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(
                ex,
                "Unauthorized access attempt at {Path}",
                SanitizePath(context.Request.Path)
            );
            await HandleExceptionAsync(
                context,
                ex,
                HttpStatusCode.Unauthorized,
                "You are not authorized to perform this action."
            );
        }

        catch (KeyNotFoundException ex)
        {
            _logger.LogInformation(
                ex,
                "Resource not found at {Path}: {Message}",
                SanitizePath(context.Request.Path),
                SanitizeForLogging(ex.Message)
            );
            await HandleExceptionAsync(
                context,
                ex,
                HttpStatusCode.NotFound,
                "The requested resource was not found."
            );
        }

        catch (ArgumentNullException ex)
        {
            // Must be before ArgumentException (inheritance hierarchy)
            _logger.LogInformation(
                ex,
                "Missing required parameter at {Path}: {ParamName}",
                SanitizePath(context.Request.Path),
                ex.ParamName
            );
            await HandleExceptionAsync(
                context,
                ex,
                HttpStatusCode.BadRequest,
                $"Required parameter is missing: {ex.ParamName}"
            );
        }

        catch (ArgumentException ex)
        {
            // ArgumentException used for "not found" in services (e.g., "Course not found")
            // Map to 404 if message contains "not found", otherwise 400
            var statusCode = ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? HttpStatusCode.NotFound
                : HttpStatusCode.BadRequest;

            _logger.LogInformation(
                ex,
                "Argument validation failed at {Path}: {Message}",
                SanitizePath(context.Request.Path),
                SanitizeForLogging(ex.Message)
            );

            var sanitizedMessage = SanitizeExceptionMessage(ex.Message);
            await HandleExceptionAsync(context, ex, statusCode, sanitizedMessage);
        }

        catch (InvalidOperationException ex)
        {
            // InvalidOperationException used for locked resources and business rule violations
            // Map to 403 if message contains "locked" or "cannot", otherwise 400
            var statusCode =
                ex.Message.Contains("locked", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("cannot", StringComparison.OrdinalIgnoreCase)
                    ? HttpStatusCode.Forbidden
                    : HttpStatusCode.BadRequest;

            _logger.LogWarning(
                ex,
                "Operation failed at {Path}: {Message}",
                SanitizePath(context.Request.Path),
                SanitizeForLogging(ex.Message)
            );

            var sanitizedMessage = SanitizeExceptionMessage(ex.Message);
            await HandleExceptionAsync(context, ex, statusCode, sanitizedMessage);
        }

        catch (FormatException ex)
        {
            _logger.LogInformation(
                ex,
                "Format error at {Path}",
                SanitizePath(context.Request.Path)
            );
            await HandleExceptionAsync(
                context,
                ex,
                HttpStatusCode.BadRequest,
                "Invalid data format provided."
            );
        }

        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(
                ex,
                "Concurrency conflict at {Path}",
                SanitizePath(context.Request.Path)
            );
            await HandleExceptionAsync(
                context,
                ex,
                HttpStatusCode.Conflict,
                "The resource was modified by another user. Please refresh and try again."
            );
        }

        catch (DbUpdateException ex)
        {
            _logger.LogError(
                ex,
                "Database error at {Path}: {Message}",
                SanitizePath(context.Request.Path),
                SanitizeForLogging(ex.InnerException?.Message ?? ex.Message)
            );

            // Check for specific SQL errors (unique constraint, FK violation, etc.)
            var message =
                ex.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
                == true
                    ? "A record with this value already exists."
                    : "A database error occurred while processing your request.";

            await HandleExceptionAsync(context, ex, HttpStatusCode.InternalServerError, message);
        }
        
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception at {Path}",
                SanitizePath(context.Request.Path)
            );
            await HandleExceptionAsync(
                context,
                ex,
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception,
        HttpStatusCode statusCode,
        string message
    )
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse
        {
            Message = message,
            StatusCode = (int)statusCode,
            // Only include stack trace in development environment
            Detail = _env.IsDevelopment() ? SanitizeForLogging(exception.StackTrace) : null,
        };

        await context.Response.WriteAsJsonAsync(response);
    }

    /// <summary>
    /// Sanitizes request path for logging to prevent log injection attacks.
    /// Removes control characters, newlines, and limits length.
    /// </summary>
    private static string SanitizePath(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return "(empty)";

        const int MaxLength = 200;
        var sanitized = Regex.Replace(path, @"[\r\n\t\x00-\x1F\x7F]", "");
        return sanitized.Length > MaxLength ? sanitized[..MaxLength] + "..." : sanitized;
    }

    /// <summary>
    /// Sanitizes strings for logging by removing control characters and limiting length.
    /// Used for exception messages, stack traces, and other potentially untrusted content.
    /// </summary>
    private static string? SanitizeForLogging(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        const int MaxLength = 1000;
        var sanitized = Regex.Replace(input, @"[\r\n\t\x00-\x1F\x7F]", " ");
        return sanitized.Length > MaxLength ? sanitized[..MaxLength] + "..." : sanitized;
    }

    /// <summary>
    /// Sanitizes exception messages before exposing to clients.
    /// Removes potentially sensitive information like file paths.
    /// </summary>
    private static string SanitizeExceptionMessage(string? message)
    {
        if (string.IsNullOrEmpty(message))
            return "An error occurred";

        // Remove common path patterns (Windows and Unix)
        var sanitized = Regex.Replace(
            message,
            @"(?:(?:[A-Za-z]:\\|/)[^\s:]+)",
            "[path]",
            RegexOptions.IgnoreCase
        );

        // Remove connection strings or anything that looks like a DSN
        sanitized = Regex.Replace(
            sanitized,
            @"(Server|Data Source|Initial Catalog|User ID|Password|Host|Port)=[^;]+",
            "$1=[redacted]",
            RegexOptions.IgnoreCase
        );

        return SanitizeForLogging(sanitized) ?? "An error occurred";
    }

    /// <summary>
    /// Standardized error response shape returned by all error handlers.
    /// </summary>
    private record ErrorResponse
    {
        public required string Message { get; init; }
        public required int StatusCode { get; init; }
        public string? Detail { get; init; }
    }
}
