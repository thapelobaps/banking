namespace Kape.Api.Exceptions;

public abstract class ApiException(
    int statusCode,
    string title,
    string message,
    IReadOnlyDictionary<string, string[]>? errors = null)
    : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Title { get; } = title;
    public IReadOnlyDictionary<string, string[]>? Errors { get; } = errors;
}

public sealed class ValidationApiException(IReadOnlyDictionary<string, string[]> errors)
    : ApiException(
        StatusCodes.Status400BadRequest,
        "Validation failed",
        "One or more validation errors occurred.",
        errors);

public sealed class UnauthorizedApiException(string message = "Authentication failed.")
    : ApiException(StatusCodes.Status401Unauthorized, "Unauthorized", message);

public sealed class NotFoundApiException(string message)
    : ApiException(StatusCodes.Status404NotFound, "Not found", message);

public sealed class ConflictApiException(string message)
    : ApiException(StatusCodes.Status409Conflict, "Conflict", message);

public sealed class TooManyRequestsApiException(string message)
    : ApiException(StatusCodes.Status429TooManyRequests, "Too many requests", message);
