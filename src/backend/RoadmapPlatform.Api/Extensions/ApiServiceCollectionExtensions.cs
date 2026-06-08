namespace RoadmapPlatform.Api.Extensions
{
    // Class này dùng để đăng ký các service chung thuộc tầng API.
    // Những phần nên đặt ở đây gồm: Controllers, CORS, HttpContextAccessor,
    // cấu hình JSON, cấu hình model validation, và các setup API chung.
    // Không đặt cấu hình Authentication/Authorization lớn ở đây để tránh file bị phình to.
    public static class ApiServiceCollectionExtensions
    {
        public static IServiceCollection AddApiServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var allowedOrigins = configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? [];

            services.AddHttpClient();

            services.AddResponseCompression();

            services.AddControllers();

            services.AddHttpContextAccessor();

            services.AddCors(options =>
            {
                options.AddPolicy("DefaultCorsPolicy", policy =>
                {
                    policy
                        .WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            return services;
        }
    }
}