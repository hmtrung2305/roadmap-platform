using RoadmapPlatform.Application.Exceptions;

namespace RoadmapPlatform.Api.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                context.Response.ContentType = "application/json";

                context.Response.StatusCode = ex switch
                {
                    ConflictException => StatusCodes.Status409Conflict,
                    UnauthorizedException => StatusCodes.Status401Unauthorized,
                    ForbiddenException => StatusCodes.Status403Forbidden,
                    EmailNotVerifiedException => StatusCodes.Status403Forbidden,
                    NotFoundException => StatusCodes.Status404NotFound,
                    ArgumentException => StatusCodes.Status400BadRequest,
                    InvalidOperationException => StatusCodes.Status400BadRequest,
                    _ => StatusCodes.Status500InternalServerError
                };

                object response = ex switch
                {
                    EmailNotVerifiedException emailEx => new
                    {
                        code = "EMAIL_NOT_VERIFIED",
                        message = emailEx.Message,
                        requiresEmailVerification = true,
                        email = emailEx.Email
                    },

                    _ => new
                    {
                        message = ex.Message
                    }
                };

                await context.Response.WriteAsJsonAsync(response);
            }
        }
    }
}
