using RoadmapPlatform.Api.Middleware;

namespace RoadmapPlatform.Api.Extensions
{
    // Class này dùng để cấu hình pipeline xử lý HTTP request.
    // Những phần nên đặt ở đây gồm: HTTPS redirection, exception middleware,
    // CORS middleware, Authentication middleware, Authorization middleware,
    // và mapping controllers.
    public static class ApiApplicationBuilderExtensions
    {
        public static WebApplication UseApiPipeline(this WebApplication app)
        {
            app.UseHttpsRedirection();

            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.UseCors("DefaultCorsPolicy");

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseStaticFiles();

            app.MapControllers();

            return app;
        }
    }
}