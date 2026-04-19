using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace ImageProcessingService.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled request failure for {Path}", context.Request.Path);
            await WriteErrorResponseAsync(context, exception);
        }
    }

    private static async Task WriteErrorResponseAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            InvalidOperationException => ((int)HttpStatusCode.BadRequest, "Request failed"),
            DbUpdateException => ((int)HttpStatusCode.Conflict, "Database update conflict"),
            FileNotFoundException => ((int)HttpStatusCode.NotFound, "Resource not found"),
            UnauthorizedAccessException => ((int)HttpStatusCode.Forbidden, "Access denied"),
            _ => ((int)HttpStatusCode.InternalServerError, "Unexpected server error")
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var payload = JsonSerializer.Serialize(new
        {
            title,
            detail = exception.Message,
            status = statusCode
        });

        await context.Response.WriteAsync(payload);
    }
}
