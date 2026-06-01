using RoadmapPlatform.Api.Extensions;
using RoadmapPlatform.Application.Extensions;
using RoadmapPlatform.Infrastructure.Extensions;

namespace RoadmapPlatform.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services
                .AddApiServices(builder.Configuration)
                .AddApiAuthentication(builder.Configuration)
                .AddApiAuthorization()
                .AddApplicationServices()
                .AddInfrastructureServices(builder.Configuration);

            var app = builder.Build();

            app.UseApiPipeline();

            app.Run();
        }
    }
}