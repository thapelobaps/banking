using Kape.Api.Exceptions;

namespace Kape.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ApiException exception)
        {
            await WriteApiErrorAsync(context, exception);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            context.Response.StatusCode = 499;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled API exception. TraceId: {TraceId}", context.TraceIdentifier);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://httpstatuses.com/500",
                title = "Unexpected server error",
                status = StatusCodes.Status500InternalServerError,
                detail = "An unexpected error occurred.",
                traceId = context.TraceIdentifier,
            });
        }
    }

    private static async Task WriteApiErrorAsync(
        HttpContext context,
        ApiException exception)
    {
        context.Response.StatusCode = exception.StatusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(new
        {
            type = $"https://httpstatuses.com/{exception.StatusCode}",
            title = exception.Title,
            status = exception.StatusCode,
            detail = exception.Message,
            errors = exception.Errors,
            traceId = context.TraceIdentifier,
        });
    }
}
