# Project Structure

## Backend

Backend được tổ chức theo mô hình nhiều project trong cùng một solution:

```text
src/
├── RoadmapPlatform.Api/
├── RoadmapPlatform.Application/
└── RoadmapPlatform.Infrastructure/
```

Cách chia này giúp tách rõ phần nhận request, phần contract dùng chung, và phần implementation có phụ thuộc database hoặc service bên ngoài.

---

## Dependency direction

```text
RoadmapPlatform.Api
  ├── references RoadmapPlatform.Application
  └── references RoadmapPlatform.Infrastructure

RoadmapPlatform.Infrastructure
  └── references RoadmapPlatform.Application

RoadmapPlatform.Application
  └── không reference project nội bộ nào khác
```

Không tạo reference ngược từ `Application` sang `Infrastructure`, vì như vậy sẽ làm rối dependency hoặc gây circular reference.

---

## `RoadmapPlatform.Api`

```text
RoadmapPlatform.Api/
├── Controllers/
├── Middleware/
├── Extensions/
├── Program.cs
└── appsettings.json
```

Project này là HTTP entry point của backend.

Nhiệm vụ chính:
- Nhận request từ frontend.
- Gọi service thông qua interface trong `Application`.
- Trả response về frontend.
- Cấu hình middleware, authentication, authorization, CORS, Swagger, DI trong `Program.cs` hoặc `Extensions`.

Không nên đặt business logic hoặc database logic trực tiếp trong controller.

Ví dụ controller nên inject interface:

```csharp
private readonly IAuthService _authService;
```

Không inject trực tiếp implementation như:

```csharp
private readonly AuthService _authService;
```

---

## `RoadmapPlatform.Application`

```text
RoadmapPlatform.Application/
├── Constants/
├── DTOs/
├── Interfaces/
├── Exceptions/
└── Common/          optional
```

Project này hiện tại đóng vai trò là contract layer.

Nó chứa những thứ các project khác cần dùng chung:
- DTO request/response.
- Interface của service.
- Constant dùng chung.
- Custom exception nếu cần dùng ở nhiều tầng.
- Common helper/model không phụ thuộc Infrastructure.

Ví dụ đúng:

```text
RoadmapPlatform.Application/
├── DTOs/Auth/LoginRequestDto.cs
├── DTOs/Auth/LoginResponseDto.cs
├── DTOs/Users/UserResponseDto.cs
├── Interfaces/IAuthService.cs
├── Interfaces/IJwtTokenService.cs
└── Constants/AuthProviders.cs
```

---

## `RoadmapPlatform.Infrastructure`

```text
RoadmapPlatform.Infrastructure/
├── Data/
├── Entities/
├── Configurations/
├── Services/
└── Clients/         optional
```

Project này chứa implementation thật của các interface được định nghĩa trong `Application`.

Vì hiện tại hầu hết service đều dùng `ApplicationDbContext`, các service implementation nên nằm trong `Infrastructure/Services`.

Ví dụ:

```text
RoadmapPlatform.Infrastructure/
├── Data/ApplicationDbContext.cs
├── Entities/User.cs
├── Entities/UserProfile.cs
├── Entities/UserAuthProvider.cs
├── Configurations/JwtSettings.cs
└── Services/
    ├── AuthService.cs
    ├── OAuthLoginService.cs
    ├── AuthProviderService.cs
    ├── EmailVerificationService.cs
    └── JwtTokenService.cs
```

Các service trong `Infrastructure` có thể dùng:
- `ApplicationDbContext`
- EF entities
- EF Core

Ví dụ:

```csharp
public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _dbContext;

    public AuthService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }
}
```

---

## Service registration

Service implementation nằm trong `Infrastructure`, nhưng controller vẫn dùng interface từ `Application`.

Ví dụ DI trong `InfrastructureServiceCollectionExtensions.cs`:

```csharp
services.AddScoped<IAuthService, AuthService>();
services.AddScoped<IOAuthLoginService, OAuthLoginService>();
services.AddScoped<IJwtTokenService, JwtTokenService>();
services.AddScoped<IAuthProviderService, AuthProviderService>();
services.AddScoped<IEmailVerificationService, EmailVerificationService>();
```

Trong `Program.cs`, chỉ gọi extension method:

```csharp
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
```

---

## Current rule for services

Nếu service cần dùng `DbContext`, EF entity, JWT config, email sender, hoặc external API client, đặt implementation trong:

```text
RoadmapPlatform.Infrastructure/Services/
```

Nếu file chỉ là interface hoặc DTO, đặt trong:

```text
RoadmapPlatform.Application/
```

Rule ngắn gọn:

```text
Application = contracts, DTOs, constants
Infrastructure = implementations, database, external systems
Api = controllers, middleware, HTTP setup
```

---

## Frontend

Frontend structure sẽ được thống nhất sau. Hiện tại file này chỉ mô tả backend structure.
