namespace RoadmapPlatform.Api.Responses;

/// <summary>
/// Represents the standard eror response returned by the API.
/// </summary>
/// <remarks>
/// This model is used to keep error reponses consistent across the backend.
///
/// It can represent different error types such as:
/// - Validation errors.
/// - Authentication errors.
/// - Authorization errors.
/// - Rate limit errors.
/// - Business rule errors.
/// - Unexpected server errors.
///
/// This class only defines the response shape.
/// The logic that creates and maps errors into this model should stay in
/// ApiErrorResponseFactory or middleware/controller code.
/// </remarks>
public sealed class ApiErrorResponse
{
    /// <summary>
    /// Gets the human-readable error message.
    /// </summary>
    /// <remarks>
    /// This message can be displayed to users or developers depending on the error type.
    /// Frontend business logic should prefer Code instead of parsing this text.
    /// </remarks>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the human-readable error message.
    /// </summary>
    /// <remarks>
    /// This message can be displayed to users or developers depending on the error type.
    /// Frontend business logic should prefer Code instead of parsing this text.
    /// </remarks>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the HTTP status code returned with this error.
    /// </summary>
    /// <remarks>
    /// This should match the actual HTTP response status code.
    ///
    /// Example values:
    /// - 400 for bad requests.
    /// - 401 for unauthenticated requests.
    /// - 403 for forbidden requests.
    /// - 404 for missing resources.
    /// - 429 for rate limit errors.
    /// - 500 for unexpected server errors.
    /// </remarks>
    public required int Status { get; init; }

    /// <summary>
    /// Gets optional additional error details.
    /// </summary>
    /// <remarks>
    /// This field can contain extra structured information about the error.
    ///
    /// It should be used carefully because object? gives flexibility but
    /// reducts type safety.
    /// </remarks>
    public object? Details { get; init; }

    /// <summary>
    /// Gets get validation errors grouped by field name.
    /// </summary>
    /// <remarks>
    /// This is mainly used for model validation errors.
    ///
    /// Example:
    /// {
    ///     "email": [ "Email is required." ],
    ///     "password": [ "Password must be at least 8 characters." ]
    /// }
    /// .
    /// </remarks>
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }

    /// <summary>
    /// Gets the number of seconds the client should wait before retrying.
    /// </summary>
    /// <remarks>
    /// This is mainly used for rate limit responses such as HTTP 429.
    /// When this value is present, the frontend can show a retry countdown
    /// or delay the next request.
    /// </remarks>
    public int? RetryAfterSeconds { get; init; }

    /// <summary>
    /// Gets optional AI credit information related to the error.
    /// </summary>
    /// <remarks>
    /// This field can be used when a request fails because of AI credit limits,
    /// quota exhaustion, or related AI usage restrictions.
    ///
    /// It is typed as object? to allow different credit reponse shapes.
    /// </remarks>
    public object? CreditStatus { get; init; }

    /// <summary>
    /// Gets the request trace indetifier.
    /// </summary>
    /// <remarks>
    /// This value helps connect a frontend error report to backend logs.
    /// It is useful for debugging production issues.
    /// </remarks>
    public string? TraceId { get; init; }
}
