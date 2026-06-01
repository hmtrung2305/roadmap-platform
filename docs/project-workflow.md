# ASP.NET Project Workflow
*Brought to you by ChatGPT with some light editing*

Tài liệu này quy định workflow phát triển backend cho `RoadmapPlatform`, đặc biệt là cách tổ chức solution, xử lý `Program.cs`, extension methods, database-first scaffolding và quy trình thêm feature mới.

Trước khi đọc tiếp thì mọi người nên tìm hiểu thêm về Extensions trong C#, đặc biệt là cách dùng extension methods để chia nhỏ phần cấu hình trong `Program.cs`.

Hiện tại trong cấu trúc project ban đầu đã có các extension cơ bản. Khi làm tiếp, vẫn có thể thêm extension mới nếu một file extension trở nên quá dài hoặc khó quản lý.

Ví dụ: ban đầu có thể để Authentication và Authorization trong `ApiServiceCollectionExtensions.cs`, nhưng nếu phần Authentication có JWT, Google OAuth, GitHub OAuth, external cookie, token validation rồi sau này thêm các policy của phân quyền thì file sẽ rất dài. Vì vậy tách thành `ApiAuthenticationExtensions.cs` và `ApiAuthorizationExtensions.cs`.
## 1. Mục tiêu kiến trúc

Backend được chia thành 3 project chính:

```text
RoadmapPlatform.Api
RoadmapPlatform.Application
RoadmapPlatform.Infrastructure
```

Ý nghĩa:

- `Api`: nhận HTTP request, gọi service, trả HTTP response.
- `Application`: chứa business logic, DTOs, interfaces, constants, exceptions.
- `Infrastructure`: chứa database, scaffolded entities, DbContext, external services.

Dependency direction chuẩn:

```text
Api -> Application
Api -> Infrastructure
Infrastructure -> Application
```

Không tạo dependency này:

```text
Application -> Api
Application -> Infrastructure
```

`Application` phải là tầng trung tâm và không phụ thuộc vào chi tiết bên ngoài.

## 2. Program.cs rule

`Program.cs` chỉ nên là composition root.

Nhiệm vụ chính:
- Tạo builder.
- Gọi các extension method để đăng ký services.
- Build app.
- Gọi extension method để cấu hình middleware pipeline.
- Run app.

Không trực tiếp nhét nhiều service registration hoặc middleware logic vào `Program.cs`.

Ví dụ flow nên có trong `Program.cs`:
```csharp
builder.Services
	// Mấy cái dưới đây là extensions
    .AddApiServices(builder.Configuration)
    .AddApiAuthentication(builder.Configuration)
    .AddApiAuthorization()
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration);

app.UseApiPipeline();
```

Quy tắc team:
```text
Program.cs gần như không nên bị sửa sau initial setup.
```

Nếu cần thêm service, middleware hoặc config mới, hãy thêm vào extension phù hợp.

## 3. API extensions workflow

Các extension của tầng API đặt tại:
```text
RoadmapPlatform.Api/Extensions
```

Các file chính:
```text
ApiServiceCollectionExtensions.cs
ApiAuthenticationExtensions.cs
ApiAuthorizationExtensions.cs
ApiApplicationBuilderExtensions.cs
```

### 3.1 ApiServiceCollectionExtensions

Dùng cho setup chung của API.

Nên đặt ở đây:
```text
Controllers
CORS
HttpContextAccessor
JSON options
API behavior options
Model validation config
```


Ví dụ các dòng setup có thể nằm ở đây:
```csharp
services.AddControllers();
services.AddHttpContextAccessor();
services.AddCors(...);
```
### 3.2 ApiAuthenticationExtensions

Dùng cho xác thực người dùng.

Nên đặt ở đây:
```text
JWT Bearer setup
Google OAuth setup
GitHub OAuth setup
External login cookie
Token validation
Authentication scheme
```

Authentication trả lời câu hỏi:
```text
Người dùng là ai?
```

Ví dụ các dòng setup có thể nằm ở đây: Cái phần này là e đã xử lý rồi
```csharp
services.AddAuthentication(...);
services.AddJwtBearer(...);
services.AddCookie("External");
services.AddGoogle(...);
services.AddGitHub(...);
```

### 3.3 ApiAuthorizationExtensions

Dùng cho phân quyền.

Nên đặt ở đây:
```text
Role policies
Permission policies
Custom authorization requirements
Authorization handlers
Fallback policy
```

Authorization trả lời câu hỏi:
```text
Người dùng này được phép làm gì?
```

*Note:* Cái này là xử lý cấu hình cho phân quyền, đừng có để tâm quá cái code AI gen ở dưới, đó để mọi người hình dung là trong đây sẽ cấu hình các permission và các thứ liên quan đến phân quyền nói chung. 

Ví dụ các dòng setup có thể nằm ở đây:
```csharp
services.AddAuthorization(...);
services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
```

Permission policy của ASP.NET Core nên đặt ở đây. Ví dụ:
```csharp
options.AddPolicy("ManageUsers", policy => policy.RequireClaim("permission", "manage_users"));
options.AddPolicy("ManageRoadmaps", policy => policy.RequireClaim("permission", "manage_roadmaps"));
```

### 3.4 ApiApplicationBuilderExtensions

Dùng cho middleware pipeline.

Nên đặt ở đây:
```text
HTTPS redirection
Exception middleware
CORS middleware
Authentication middleware
Authorization middleware
MapControllers
```

Ví dụ các dòng middleware nên nằm ở đây:
```csharp
app.UseHttpsRedirection();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("DefaultCorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

Middleware order quan trọng:
```text
UseHttpsRedirection
UseMiddleware<ExceptionHandlingMiddleware>
UseCors
UseAuthentication
UseAuthorization
MapControllers
```

## 4. Application extension workflow

Application extension đặt tại:

```text
RoadmapPlatform.Application/Extensions/ApplicationServiceCollectionExtensions.cs
```

Dùng để đăng ký business services.

Nên đặt ở đây:

```text
AuthService
UserService
RoadmapService
RoleService
PermissionService
EmailVerificationService
PortfolioService
ChatService
LearningResourceService
```

Ví dụ các dòng registration nên nằm ở đây:
```csharp
services.AddScoped<IAuthService, AuthService>();
services.AddScoped<IUserService, UserService>();
services.AddScoped<IRoadmapService, RoadmapService>();
services.AddScoped<IRoleService, RoleService>();
services.AddScoped<IPermissionService, PermissionService>();
services.AddScoped<IEmailVerificationService, EmailVerificationService>();
```

Rule:
```text
Khi thêm business service mới, đăng ký ở ApplicationServiceCollectionExtensions.cs.
Không sửa Program.cs.
```

## 5. Infrastructure extension workflow

Infrastructure extension đặt tại:

```text
RoadmapPlatform.Infrastructure/Extensions/InfrastructureServiceCollectionExtensions.cs
```

Dùng để đăng ký database và external services.

Nên đặt ở đây:
```text
DbContext
Database provider
Email sender
GitHub API client
RAG service
Python service client
Job market analysis client
File storage
External API clients
```

Ví dụ các dòng registration nên nằm ở đây:
```csharp
services.AddDbContext<ApplicationDbContext>(...);
services.AddScoped<IEmailSender, EmailSender>();
services.AddScoped<IGitHubClient, GitHubApiClient>();
```

Rule:
```text
Interface của external service đặt ở Application/Interfaces.
Implementation đặt ở Infrastructure/ExternalServices.
Registration đặt ở InfrastructureServiceCollectionExtensions.cs.
```

## 6. User Secrets

Project này dùng **ASP.NET Core User Secrets** cho local development.

Thay vì dùng `.env` cần phải cài thư viện. Với ASP.NET Core, user secrets tích hợp trực tiếp với `IConfiguration`, nên có thể đọc bằng:
```csharp
configuration["Jwt:Key"]
configuration.GetConnectionString("DefaultConnection")
configuration["Authentication:Google:ClientId"]
```
### 6.1 File nào được commit?

Được commit:
```text
appsettings.json
appsettings.Development.json
```

Không commit secret thật vào các file trên.

`appsettings.json` chỉ nên chứa config không nhạy cảm hoặc placeholder.

Ví dụ:
```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:5173"
    ]
  }
}
```

### 6.2 Secret thật đặt ở đâu?

Secret thật đặt bằng user secrets.

Chạy từ project API (không đặt user-secret ở hai cái project còn lại):
```powershell
cd RoadmapPlatform.Api
dotnet user-secrets init
```

Rồi ở bên trong Visual Studio chuột phải vào cái project `RoadmapPlatform.Api` và chọn phần `Manage User Secrets` nó sẽ hiện lên file `secrets.json`

### 6.4 Config shape tham khảo

Cái connection string thì mỗi người mỗi khác nha nên điền vô.
Còn mấy cái kia (JWT key, oauth) thì để gửi riêng sau.
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "Jwt": {
    "Key": "..."
  },
  "Authentication": {
    "Google": {
      "ClientId": "...",
      "ClientSecret": "..."
    },
    "GitHub": {
      "ClientId": "...",
      "ClientSecret": "..."
    }
  },
  "EmailVerification": {
    "HashSecret": "..."
  },
  "GoogleAI": {
    "ApiKey": "..."
  }
}
```

Rule:
```text
Local secrets -> user secrets
Shared non-secret config -> appsettings.json
Environment-specific non-secret config -> appsettings.Development.json
Không commit secret thật vào Git
```

## 7. Database scaffolding

Command cho PowerShell:
```powershell
dotnet ef dbcontext scaffold "Name=ConnectionStrings:DefaultConnection" Npgsql.EntityFrameworkCore.PostgreSQL `
  --project RoadmapPlatform.Infrastructure `
  --startup-project RoadmapPlatform.Api `
  --context RoadmapPlatformDbContext `
  --context-dir Data `
  --output-dir Entities `
  --force
```

Command cho CMD/Git Bash:
```bash
dotnet ef dbcontext scaffold "Name=ConnectionStrings:DefaultConnection" Npgsql.EntityFrameworkCore.PostgreSQL \
  --project RoadmapPlatform.Infrastructure \
  --startup-project RoadmapPlatform.Api \
  --context RoadmapPlatformDbContext \
  --context-dir Data \
  --output-dir Entities \
  --force
```

Note:
```text
Cái connection string "Name=ConnectionStrings:DefaultConnection" này, EF sẽ tự đi tìm bên trong user-secrets và thế vào.
Nếu scaffold không đọc được user secrets, tạm thời đổi "Name=ConnectionStrings:DefaultConnection" thành connection string cứng
Không commit connection string cứng vào Git.
```
