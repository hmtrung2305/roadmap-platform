namespace RoadmapPlatform.Application.Exceptions;

/// <summary>
/// Represents an application-level forbidden access error.
/// </summary>
/// <remarks>
/// This exception should be thrown when the current user is authenticated,
/// but does not have enough permission to perform the requested action.
/// 
/// The API layer maps this exception to HTTP 403 Forbidden through ApiErrorResponseFactory.
/// </remarks>
public sealed class ForbiddenException : Exception
{
    /// <summary>
    /// Creates a new forbidden exception with a custom error message.
    /// </summary>
    /// <param name="message">
    /// The error message that explains why the request is forbidden.
    /// </param>
    public ForbiddenException(string message) : base(message) { }
}
