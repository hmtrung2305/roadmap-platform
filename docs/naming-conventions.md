# Naming Conventions
*Brought to you by ChatGPT with some light editing*

Tài liệu này quy định cách đặt tên trong backend project `RoadmapPlatform` để code nhất quán, dễ đọc và dễ review.

## 1. Nguyên tắc chung

- Dùng tiếng Anh để đặt tên class, method, property, file và folder.
- Dùng `PascalCase` cho class, interface, enum-like static class, method và property.
- Dùng `camelCase` cho biến local và parameter.
- Tên phải rõ nghĩa, tránh viết tắt khó hiểu.
- Không đặt tên chung chung như `Helper`, `Manager`, `Data`, `Common`, `Utils` nếu không có ngữ cảnh rõ ràng.

Ví dụ tốt:
```csharp
EmailVerificationService
GenerateOtpAsync
normalizedEmail
UserStatuses
```

Ví dụ nên tránh:
```csharp
UserMgr
CommonHelper
DoStuff
data
x
```
## 2. Controller naming

Controller đặt theo resource hoặc chức năng chính, kết thúc bằng `Controller`.

Format:
```text
[Resource]Controller
```

Ví dụ:
```text
AuthController
MeController
UsersController
RoadmapsController
PortfoliosController
GitHubIntegrationController
```

Quy tắc:
- Controller không chứa business logic lớn.
- Controller chỉ nhận request, gọi service, trả response.
- Tên controller nên khớp với nhóm endpoint.

Ví dụ:
```csharp
public class AuthController : ControllerBase
{
}
```

## 3. Service naming

Format:
```text
[Feature]Service
```

Ví dụ:
```text
AuthService
UserService
RoadmapService
EmailVerificationService
```

Quy tắc:
- Business service đặt trong `RoadmapPlatform.Application/Services`.

## 4. DTO naming

DTO đặt trong `RoadmapPlatform.Application/DTOs`.

Tên DTO phải thể hiện mục đích sử dụng.
### Request DTO

Format:
```text
[Action][Resource]RequestDto
```

Ví dụ:
```text
LoginRequestDto
RegisterRequestDto
CreateRoadmapRequestDto
UpdateRoadmapRequestDto
VerifyEmailRequestDto
ChangePasswordRequestDto
```

### Response DTO

Format:
```text
[Resource]ResponseDto
[Action][Resource]ResponseDto
```

Ví dụ:
```text
UserResponseDto
RoadmapResponseDto
LoginResponseDto
ProfileResponseDto
```

### DTO folder grouping

Group DTO theo feature:
```text
DTOs/
  Auth/
    LoginRequestDto.cs
    RegisterRequestDto.cs
    LoginResponseDto.cs

  Users/
    UserResponseDto.cs
    UpdateUserProfileRequestDto.cs

  Roadmaps/
    CreateRoadmapRequestDto.cs
    UpdateRoadmapRequestDto.cs
    RoadmapResponseDto.cs
```

Quy tắc:
- Không đặt tên DTO quá chung như `RequestDto`, `ResponseDto`, `UserDto` nếu không rõ mục đích.
- Không trả trực tiếp scaffolded entity ra API.
- API nên trả response DTO thay vì entity từ database.

## 5. Interface naming

Interface bắt đầu bằng `I`.

Ví dụ:
```text
IAuthService
IUserService
IEmailSender
IGitHubClient
IRagService
IJobMarketAnalysisClient
```

Quy tắc:
- Interface cho business service đặt trong `Application/Interfaces`.
- Interface cho external dependency cũng đặt trong `Application/Interfaces`.
- Implementation của external dependency đặt trong `Infrastructure`.

## 6. Entity naming

Entity scaffold từ database đặt trong:
```text
RoadmapPlatform.Infrastructure/Entities
```

Tên entity dùng số ít, `PascalCase`.

Ví dụ:
```text
User
UserProfile
Roadmap
RoadmapStep
Role
Permission
```

Quy tắc:
- Không tự ý sửa nhiều trong file scaffold nếu project dùng database-first.
- Nếu database thay đổi, cập nhật script migration document rồi scaffold lại.
## 7. Constants naming

Constants đặt trong:

```text
RoadmapPlatform.Application/Constants
```

Tên class dùng dạng số nhiều nếu chứa nhiều giá trị cùng nhóm.

Ví dụ:

```text
UserStatuses
AuthProviders
RoleNames
PermissionNames
RoadmapStatuses
```

Ví dụ:

```csharp
public static class UserStatuses
{
    public const string Active = "active";
    public const string Suspended = "suspended";
    public const string Deleted = "deleted";
}
```

Quy tắc:
- Giá trị status lưu trong database nên dùng lowercase snake_case nếu có nhiều từ.
- Không hard-code string status trực tiếp nhiều nơi trong code.

Ví dụ tốt:
```csharp
user.Status = UserStatuses.Active;
```

Ví dụ nên tránh:
```csharp
user.Status = "active";
```

## 8. Exception naming

Custom exception đặt trong:
```text
RoadmapPlatform.Application/Exceptions
```

Format:
```text
[ErrorType]Exception
```

Ví dụ:
```text
NotFoundException
ConflictException
UnauthorizedException
ForbiddenException
BadRequestException
```

Quy tắc:
- Application service được phép throw custom exception.
- API middleware sẽ chuyển exception thành HTTP response phù hợp.
## 9. Helper naming

Helper đặt trong:
```text
RoadmapPlatform.Application/Helpers
```

Tên helper phải rõ trách nhiệm.

Ví dụ tốt:
```text
EmailNormalizer
OtpGenerator
SlugHelper
DateTimeHelper
```

Ví dụ nên tránh:
```text
CommonHelper
Utility
GeneralHelper
```

Quy tắc:
- Chỉ tạo helper khi logic được dùng lại ở nhiều nơi.
- Không biến `Helpers` thành nơi chứa logic nghiệp vụ lớn.
- Nếu logic đủ lớn, hãy tạo service riêng.

## 11. Method naming

Method dùng `PascalCase`.
Async method kết thúc bằng `Async`.

Ví dụ:
```csharp
LoginAsync
RegisterAsync
GetCurrentUserAsync
CreateRoadmapAsync
VerifyEmailAsync
```

Quy tắc:
- Method async phải có hậu tố `Async`.
- Tên method nên thể hiện hành động rõ ràng.

## 12. Variable naming

Local variable và parameter dùng `camelCase`.

Ví dụ:
```csharp
var normalizedEmail = email.Trim().ToLowerInvariant();
var currentUser = await userService.GetCurrentUserAsync(userId);
```

Không dùng tên quá ngắn trừ vòng lặp đơn giản.

Nên tránh:
```csharp
var u = ...;
var data = ...;
var obj = ...;
```

## 13. Route naming

Route API dùng lowercase kebab-case. Cố gắng theo tiêu chuẩn của RESTful API (mọi người nên tìm hiểu thêm về cái này)

Ví dụ:
```text
/api/auth/login
/api/auth/register
/api/me
/api/roadmaps
/api/email-verification/resend
```

Không dùng:
```text
/api/Auth/Login
/api/emailVerification/resend
```

## 14. Database naming

Database dùng **lowercase snake_case** cho **table names, column names, constraint names, index names**.  
  
Không dùng PascalCase trong database.  
### Table names  
  
Table name nên dùng **singular noun** và viết bằng `snake_case`.

Ví dụ:
```text
user  
user_profile  
user_auth_provider  
email_verification_token  
roadmap  
roadmap_step  
role  
permission  
role_permission
```

Không nên dùng
```text
User  
UserProfile  
Users  
tbl_user  
userProfiles
```
Cái này cũng làm cho việc mình query trực tiếp trên database dễ hơn. Tại vì lúc đặt tên là `User_Profile` chẳng hạn, khi query thì phải ghi là `SELECT * FROM "User_Profile"` tại vì khi mà ghi k có dấu `""` thì PostgreSQL nó sẽ tự chuyển sang lowercase `user_profile` dẫn đến việc là k tìm thấy được bảng. 

### Column names

Column name cũng dùng **lowercase snake_case**.

Ví dụ:
```text
user_id  
display_name  
email_verified_at  
created_at  
updated_at  
deleted_at  
provider_user_id  
password_hash
```

Không nên dùng:
```text
UserId
DisplayName
emailVerifiedAt
createdAt
```
## 15. Tóm tắt nhanh

```text
Controller      -> AuthController
Service         -> AuthService / IAuthService
DTO Request     -> LoginRequestDto
DTO Response    -> LoginResponseDto
Entity          -> User
DbContext       -> RoadmapPlatformDbContext
Constants       -> UserStatuses
Exception       -> NotFoundException
Helper          -> EmailNormalizer
Extension       -> ApiAuthenticationExtensions
Async Method    -> LoginAsync
API Route       -> /api/auth/login
Database Name   -> user_profile
Database Field  -> created_at
Branch          -> feature/email-verification
Commit          -> feat: add email verification service
```
