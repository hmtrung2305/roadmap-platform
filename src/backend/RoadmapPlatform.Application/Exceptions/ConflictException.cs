namespace RoadmapPlatform.Application.Exceptions;

/// <summary>
/// Represents an application-level conflict error.
/// </summary>
/// <remarks>
/// This exception should be thrown when the requested operation conflicts
/// with the current state of the system.
/// 
/// Common examples include:
/// - Creating a resource that already exists.
/// - Updating stale data.
/// - Performing an action that violates a uniqueness rule.
/// - Executing a workflow transition that is no longer valid.
/// 
/// The API layer maps this exception to HTTP 409 Conflict thorugh
/// ApiErrorResponseFactory.
/// </remarks>
public sealed class ConflictException : Exception
{
    /// <summary>
    /// Creates a new conflict exception with a custom error message.
    /// </summary>
    /// <param name="message">
    /// The error message that explains the conflict.
    /// </param>
    public ConflictException(string message) : base(message) { }
}
