namespace RoadmapPlatform.Application.Exceptions;

/// <summary>
/// Represents an application-level resource not found error.
/// </summary>
/// <remarks>
/// This exception should be thrown when a requested resource does not exist
/// or cannot be found in the current application context.
/// 
/// The API layer maps this exception to HTTP 404 Not Found through
/// ApiErrorResponseFactory
/// </remarks>
public sealed class NotFoundException : Exception
{
    /// <summary>
    /// Creates a new not found exception with a custom error message.
    /// </summary>
    /// <param name="message">
    /// The error message that explains which resource could not be found.
    /// </param>
    public NotFoundException(string message) : base(message) { }
}
