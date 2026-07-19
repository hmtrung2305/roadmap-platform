using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RoadmapPlatform.Application.Exceptions;

namespace RoadmapPlatform.Api.Responses;

/// <summary>
/// Creates standardized API error responses.
/// </summary>
/// <remarks>
/// This factory centralizes how the API converts validation errors,
/// business exceptions, and unexpected exceptions into ApiErrorResponse objects.
///
/// Keeping this logic in one place helps the backend return consistent error
/// responses across controllers, middleware, authentication, authorization,
/// and rate limiting.
/// </remarks>
public static class ApiErrorResponseFactory
{
    /// <summary>
    /// Creates a standard API error response.
    /// </summary>
    /// <param name="httpContext">
    /// The current HTTP request context.
    /// </param>
    /// <param name="status">
    /// The HTTP status code for the error response.
    /// </param>
    /// <param name="code">
    /// A machine-readable error code.
    /// </param>
    /// <param name="message">
    /// A human-readable error code.
    /// </param>
    /// <param name="details">
    /// Optional additional structured error details.
    /// </param>
    /// <param name="errors">
    /// Optional validation errors grouped by field name.
    /// </param>
    /// <param name="retryAfterSeconds">
    /// Optional retry delay in seconds, usually used for rate limiting.
    /// </param>
    /// <param name="creditStatus">
    /// Optional AI credit status information.
    /// </param>
    /// <returns>
    /// A standardized <see cref="ApiErrorResponse"/> instance.
    /// </returns>
    /// <remarks>
    /// This is the base factory method used by other error creation methods.
    /// It always attaches the current request trace ID to help with debugging.
    /// </remarks>
    public static ApiErrorResponse Create(
        HttpContext httpContext,
        int status,
        string code,
        string message,
        object? details = null,
        IReadOnlyDictionary<string, string[]>? errors = null,
        int? retryAfterSeconds = null,
        object? creditStatus = null)
    {
        return new ApiErrorResponse
        {
            Code = code,
            Message = message,
            Status = status,
            Details = details,
            Errors = errors,
            RetryAfterSeconds = retryAfterSeconds,
            CreditStatus = creditStatus,
            TraceId = GetTraceId(httpContext)
        };
    }

    /// <summary>
    /// Creates a validation error response from ASP.NET Core model state.
    /// </summary>
    /// <param name="httpContext">
    /// The current HTTP request context.
    /// </param>
    /// <param name="modelState">
    /// The model state containing validation errors.
    /// </param>
    /// <returns>
    /// A standardized validation error response with HTTP 400 status.
    /// </returns>
    /// <remarks>
    /// This method is used when request model validation fails.
    ///
    /// Field-level validation errors are converted into the Errors property.
    /// If a validation entry has an empty key, it is grouped under "request".
    /// </remarks>
    public static ApiErrorResponse FromModelState(
        HttpContext httpContext,
        ModelStateDictionary modelState)
    {
        // Extract only fields that contain validation errors.
        // Empty keys are request-level errors; empty messages receive a safe fallback.
        var errors = modelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => string.IsNullOrWhiteSpace(entry.Key) ? "request" : entry.Key,
                entry => entry.Value!.Errors
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "The input was not valid."
                        : error.ErrorMessage)
                    .ToArray());

        return Create(
            httpContext,
            StatusCodes.Status400BadRequest,
            "VALIDATION_FAILED",
            "One or more validation errors occurred.",
            errors: errors);
    }

    /// <summary>
    /// Creates an API error response from an exception.
    /// </summary>
    /// <param name="httpContext">
    /// The current HTTP request context.
    /// </param>
    /// <param name="exception">
    /// The exception that should be converted into an API error response.
    /// </param>
    /// <returns>
    /// A standardized API error response mapped from the exception type.
    /// </returns>
    /// <remarks>
    /// This method maps known application exceptions to safe and meaningful
    /// API responses.
    ///
    /// Unknown exceptions are converted into a generic internal server error
    /// response to avoid leadking sensitive implementation details.
    /// </remarks>
    public static ApiErrorResponse FromException(HttpContext httpContext, Exception exception)
    {
        // Resolve the general HTTP status, error code, and safe message first.
        var status = GetStatusCode(exception);
        var code = GetErrorCode(exception);
        var message = GetSafeMessage(exception, status);

        return exception switch
        {
            // Email verification errors need extra details so the frontend can
            // guide users through the verification flow.
            EmailNotVerifiedException emailException => Create(
                httpContext,
                status,
                code,
                emailException.Message,
                details: new
                {
                    email = emailException.Email,
                    requiresEmailVerification = true,
                    verificationPurpose = emailException.VerificationPurpose,
                    canResendVerification = emailException.CanResendVerification
                }),

            // AI credit limit errors include credit status information so the
            // frontend can show usage/quota details.
            AiCreditLimitExceededException aiCreditException => Create(
                httpContext,
                status,
                code,
                aiCreditException.Message,
                creditStatus: aiCreditException.CreditStatus),

            // GitHub integration errors may carry their own status code,
            // error code, message, and retry delay.
            GitHubIntegrationException githubException => Create(
                httpContext,
                githubException.StatusCode,
                githubException.Code,
                githubException.Message,
                retryAfterSeconds: githubException.RetryAfterSeconds),

            // Default exception response.
            _ => Create(httpContext, status, code, message)
        };
    }

    /// <summary>
    /// Maps an exception to an HTTP status code.
    /// </summary>
    /// <param name="exception">
    /// The exception to map.
    /// </param>
    /// <returns>
    /// The HTTP status code that best represents the exception.
    /// </returns>
    /// <remarks>
    /// Known business and request exceptions are mapped to 4xx response.
    /// Unknown exceptions are mapped to HTTP 500.
    /// </remarks>
    public static int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            ConflictException => StatusCodes.Status409Conflict,
            UnauthorizedException => StatusCodes.Status401Unauthorized,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            ForbiddenException => StatusCodes.Status403Forbidden,
            EmailNotVerifiedException => StatusCodes.Status403Forbidden,
            AiCreditLimitExceededException => StatusCodes.Status429TooManyRequests,
            GitHubIntegrationException githubException => githubException.StatusCode,
            NotFoundException => StatusCodes.Status404NotFound,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            FileNotFoundException => StatusCodes.Status404NotFound,
            ArgumentException => StatusCodes.Status400BadRequest,
            InvalidOperationException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    /// <summary>
    /// Maps an exception to a machine-readable error code.
    /// </summary>
    /// <param name="exception">
    /// The exception to map.
    /// </param>
    /// <returns>
    /// A stable error code that the frontend can use for conditional handling.
    /// </returns>
    /// <remarks>
    /// Error codes should be more stable than error messages.
    /// Frontend code should prefer checking this value instead of parsing Message.
    /// </remarks>
    private static string GetErrorCode(Exception exception)
    {
        return exception switch
        {
            ConflictException => "CONFLICT",
            UnauthorizedException => "UNAUTHORIZED",
            UnauthorizedAccessException => "UNAUTHORIZED",
            ForbiddenException => "FORBIDDEN",
            EmailNotVerifiedException => "EMAIL_NOT_VERIFIED",
            AiCreditLimitExceededException => "AI_CREDIT_LIMIT_EXCEEDED",
            GitHubIntegrationException githubException => githubException.Code,
            NotFoundException => "NOT_FOUND",
            KeyNotFoundException => "NOT_FOUND",
            FileNotFoundException => "NOT_FOUND",
            ArgumentException => "INVALID_REQUEST",
            InvalidOperationException => "INVALID_REQUEST",
            _ => "INTERNAL_SERVER_ERROR"
        };
    }

    /// <summary>
    /// Gets a safe error message for the response.
    /// </summary>
    /// <param name="exception">
    /// The original exception.
    /// </param>
    /// <param name="status">
    /// The mapped HTTP status code.
    /// </param>
    /// <returns>
    /// A safe message that can be returned to the client.
    /// </returns>
    /// <remarks>
    /// For HTTP 500 errors, this method hides the real exception message
    /// to avoid exposing internal implementation details.
    ///
    /// For non-500 errors, the exception message is returned because these
    /// are usually expected business or request errors.
    /// </remarks>
    private static string GetSafeMessage(Exception exception, int status)
    {
        return status == StatusCodes.Status500InternalServerError
            ? "An unexpected error occurred."
            : exception.Message;
    }

    /// <summary>
    /// Gets the trace identifier for the current request.
    /// </summary>
    /// <param name="httpContext">
    /// The current HTTP request context.
    /// </param>
    /// <returns>
    /// The active diagnostic activity ID if available; otherwise, the HTTP trace identifier.
    /// </returns>
    /// <remarks>
    /// The trace ID helps connect frontend error reports to backend logs.
    /// </remarks>
    private static string GetTraceId(HttpContext httpContext)
    {
        return Activity.Current?.Id ?? httpContext.TraceIdentifier;
    }
}
