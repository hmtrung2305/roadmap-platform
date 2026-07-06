using RoadmapPlatform.Api.Responses;

namespace RoadmapPlatform.Api.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                if (context.Response.HasStarted)
                {
                    throw;
                }

                var response = ApiErrorResponseFactory.FromException(context, ex);

                if (response.Status >= StatusCodes.Status500InternalServerError)
                {
                    _logger.LogError(
                        ex,
                        "Unhandled exception while processing request {Method} {Path}.",
                        context.Request.Method,
                        context.Request.Path);
                }

                context.Response.Clear();
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = response.Status;

                await context.Response.WriteAsJsonAsync(response);
            }
        }
    }
}
