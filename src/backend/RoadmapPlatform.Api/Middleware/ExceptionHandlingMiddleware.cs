using RoadmapPlatform.Api.Responses;

namespace RoadmapPlatform.Api.Middleware
{
    /// <summary>
    /// Handles unhandled exceptions from the API request pipeline.
    /// </summary>
    /// <remarks>
    /// This middleware catches exceptions thrown by later middleware, controllers,
    /// or services, then converts them into the project's standard API error response.
    ///
    /// It should be registered early enough in the request pipeline to wrap most
    /// application-level request processing.
    /// </remarks>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        /// <summary>
        /// Creates a new exception handling middleware instance.
        /// </summary>
        /// <param name="next">
        /// The next middleware in the HTTP request pipeline.
        /// </param>
        /// <param name="logger">
        /// The logger used to record unhandled server-side exceptions.
        /// </param>
        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Executes the middleware for the current HTTP request.
        /// </summary>
        /// <param name="context">
        /// The current HTTP request context.
        /// </param>
        /// <returns>
        /// A task representing the asynchronous middleware execution.
        /// </returns>.
        /// <remarks>
        /// If the next middleware or controller completes successfully, this method
        /// does nothing extra.
        ///
        /// If an exception is thrown, the middleware converts it into a standardized
        /// JSON error response unless the HTTP response has already started.
        /// </remarks>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Continue to the next middleware or endpoint.
                await _next(context);
            }
            catch (Exception ex)
            {
                // If the response has already started, it is no longer safe to replace it.
                // Re-throw the exception so the server/runtime can handle it.
                if (context.Response.HasStarted)
                {
                    throw;
                }

                // Convert the exception into the project's standard API error response.
                var response = ApiErrorResponseFactory.FromException(context, ex);

                // Log only server-side errors.
                // Client-side errors such as validation or bad request errors should not
                // usually be logged as unhandled server failures.
                if (response.Status >= StatusCodes.Status500InternalServerError)
                {
                    _logger.LogError(
                        ex,
                        "Unhandled exception while processing request {Method} {Path}.",
                        context.Request.Method,
                        context.Request.Path);
                }

                // Clear any partially prepared response before writing the error response.
                context.Response.Clear();

                // Return a JSON error reponse with the mapped HTTP status code.
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = response.Status;

                await context.Response.WriteAsJsonAsync(response);
            }
        }
    }
}
