using Microsoft.Extensions.DependencyInjection;

namespace RoadmapPlatform.Application.Extensions
{
    // Class này dùng để đăng ký các service thuộc tầng Application.
    // Những phần nên đặt ở đây gồm: business services, use-case services,
    // service xử lý auth, user, roadmap, role, permission, email verification,
    // learning resource, chat, portfolio, và các logic nghiệp vụ chính.
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Đăng ký service nghiệp vụ ở đây sau khi tạo interface và implementation.
            // Ví dụ:
            // services.AddScoped<IAuthService, AuthService>();
            // services.AddScoped<IUserService, UserService>();
            // services.AddScoped<IRoadmapService, RoadmapService>();
            // services.AddScoped<IRoleService, RoleService>();
            // services.AddScoped<IPermissionService, PermissionService>();

            return services;
        }
    }
}