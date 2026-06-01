namespace RoadmapPlatform.Api.Extensions
{
    // Class này dùng để cấu hình Authorization cho API.
    // Những phần nên đặt ở đây gồm: role policies, permission policies,
    // custom authorization requirements, authorization handlers, fallback policy,
    // và các rule kiểm tra quyền truy cập endpoint.
    // Authorization trả lời câu hỏi: "Người dùng này được phép làm gì?"
    public static class ApiAuthorizationExtensions
    {
        public static IServiceCollection AddApiAuthorization(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                // Các policy phân quyền sẽ được thêm sau.
                // Ví dụ:
                // options.AddPolicy("ManageUsers", policy =>
                // {
                //     policy.RequireClaim("permission", "manage_users");
                // });
                //
                // options.AddPolicy("AdminOnly", policy =>
                // {
                //     policy.RequireRole("admin");
                // });
            });

            return services;
        }
    }
}