using RoadmapPlatform.Api.Extensions;
using RoadmapPlatform.Application.Extensions;
using RoadmapPlatform.Infrastructure.Extensions;

namespace RoadmapPlatform.Api
{
    /// <summary>
    /// Entry point API.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main function.
        /// </summary>
        /// <param name="args">Arguments from command line.</param>
        public static void Main(string[] args)
        {
            // Create the ASP.NET Core application builder.
            var builder = WebApplication.CreateBuilder(args);

            // Register services by application layer.
            // Detailed configuration is defined inside each extension method.
            builder.Services
                .AddApiServices(builder.Configuration)
                .AddApiAuthentication(builder.Configuration)
                .AddApiAuthorization()
                .AddApplicationServices()
                .AddInfrastructureServices(builder.Configuration)
                .AddEmailServices(builder.Configuration);

            // Build the application after all services are registered.
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.UseApiPipeline();

            // Start the web application.
            app.Run();
        }
    }
}