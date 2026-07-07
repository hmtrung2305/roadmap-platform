namespace RoadmapPlatform.Application.Exceptions;

/// <summary>
/// Represents an application-level unauthorized error.
/// </summary>
/// <remarks>
/// This exception should be thrown when a request cannot be completed because
/// the current user is not authenticated or the authentication context is invalid.
/// 
/// The API layer maps this exception to HTTP 401 Unauthorized through
/// ApiErrorResponseFactory.
/// </remarks>
public class UnauthorizedException : Exception
{
    /// <summary>
    /// Creates a new unauthorized exception with a custom error message.
    /// </summary>
    /// <param name="message">
    /// The error message that explains why the request is unauthorized.
    /// </param>
    public UnauthorizedException(string message) : base(message) { }
}
