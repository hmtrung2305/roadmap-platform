using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RoadmapPlatform.Application.Exceptions;

namespace RoadmapPlatform.Api.Responses;

public static class ApiErrorResponseFactory
{
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

    public static ApiErrorResponse FromModelState(
        HttpContext httpContext,
        ModelStateDictionary modelState)
    {
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

    public static ApiErrorResponse FromException(HttpContext httpContext, Exception exception)
    {
        var status = GetStatusCode(exception);
        var code = GetErrorCode(exception);
        var message = GetSafeMessage(exception, status);

        return exception switch
        {
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

            AiCreditLimitExceededException aiCreditException => Create(
                httpContext,
                status,
                code,
                aiCreditException.Message,
                creditStatus: aiCreditException.CreditStatus),

            GitHubIntegrationException githubException => Create(
                httpContext,
                githubException.StatusCode,
                githubException.Code,
                githubException.Message,
                retryAfterSeconds: githubException.RetryAfterSeconds),

            _ => Create(httpContext, status, code, message)
        };
    }

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

    private static string GetSafeMessage(Exception exception, int status)
    {
        return status == StatusCodes.Status500InternalServerError
            ? "An unexpected error occurred."
            : exception.Message;
    }

    private static string GetTraceId(HttpContext httpContext)
    {
        return Activity.Current?.Id ?? httpContext.TraceIdentifier;
    }
}
