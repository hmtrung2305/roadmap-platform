# Database Workflow
*Brought to you by ChatGPT with some light editing*

Tài liệu này quy định cách team quản lý database thông qua các file trong `docs/database`.

## 1. Cấu trúc thư mục database docs

```text
docs/
└── database/
    ├── schema.sql
    ├── seed.sql
    └── migrations/
        ├── 001-initial-schema.sql
        ├── 002-user-streak-properties.sql
        └── 003-add-resource-tables.sql
```

## 2. Vai trò của từng file

### `schema.sql`

Chứa cấu trúc database hiện tại.

Bao gồm:

```text
CREATE TABLE
PRIMARY KEY
FOREIGN KEY
INDEX
CONSTRAINT
ENUM/type nếu có
```

File này nên thể hiện trạng thái mới nhất của database.

### `seed.sql`

Chứa dữ liệu mặc định hoặc dữ liệu mẫu cần có khi setup project.

Ví dụ:

```text
Default roles
Default permissions
Role-permission mapping
Sample data cần thiết
```

Không đưa dữ liệu cá nhân thật, password thật hoặc secret vào `seed.sql`.

### `migrations/`

Chứa lịch sử thay đổi database theo từng file nhỏ.

Mỗi khi có thay đổi database, tạo một file migration mới để ghi lại thay đổi đó.

Ví dụ:

```text
001-initial-schema.sql
002-user-streak-properties.sql
003-add-resource-tables.sql
004-add-avatar-url-to-user-profile.sql
```

## 3. Quy tắc đặt tên migration file

Format:

```text
<number>-<short-description>.sql
```

Ví dụ:

```text
001-initial-schema.sql
002-user-streak-properties.sql
003-add-resource-tables.sql
004-add-email-verification-fields.sql
005-add-deleted-at-to-users.sql
```

Quy tắc:

- Số thứ tự tăng dần.
- Dùng 3 chữ số: `001`, `002`, `003`.
- Tên file viết thường.
- Dùng dấu `-` thay vì khoảng trắng.
- Tên phải mô tả rõ thay đổi.

Không nên đặt tên như:

```text
002-update.sql
003-fix.sql
004-new-changes.sql
```

## 4. Khi nào cần tạo migration file?

Cần tạo file mới trong `migrations/` khi có thay đổi như:

```text
Thêm table mới
Thêm column mới
Sửa kiểu dữ liệu column
Đổi tên column/table
Thêm foreign key
Thêm index
Thêm constraint
Thêm enum/type mới
Thêm seed data quan trọng
```

Ví dụ:

```text
Thêm streak_count vào user_profiles
Thêm deleted_at vào users
Thêm bảng resources
Thêm bảng resource_chunks
Thêm status cho learning_resources
```

## 5. Workflow khi thay đổi database

### Bước 1: Tạo migration file mới

Ví dụ muốn thêm streak properties cho user:
```text
docs/database/migrations/002-user-streak-properties.sql
```

Nội dung ví dụ:
```sql
ALTER TABLE user_profiles
ADD COLUMN streak_count integer NOT NULL DEFAULT 0;

ALTER TABLE user_profiles
ADD COLUMN last_study_date date NULL;
```

### Bước 2: Cập nhật `schema.sql`

Sau khi viết migration, cập nhật lại `schema.sql` để phản ánh cấu trúc database mới nhất.

Ví dụ nếu migration thêm column:
```sql
streak_count integer NOT NULL DEFAULT 0,
last_study_date date NULL,
```

thì trong `schema.sql` cũng phải có hai column này.

### Bước 3: Cập nhật `seed.sql` nếu cần

Nếu thay đổi cần dữ liệu mặc định, cập nhật `seed.sql`.

Ví dụ:
```sql
INSERT INTO roles (role_name)
VALUES ('Student'), ('Admin');
```

Nếu không có seed data mới thì bỏ qua bước này.

### Bước 4: Chạy script trên local database

Chạy migration trên database local để kiểm tra.

Ví dụ:
```bash
psql -U postgres -d database_name -f docs/database/migrations/002-user-streak-properties.sql
```

Hoặc copy script vào pgAdmin để chạy.

### Bước 5: Scaffold lại model nếu cần

Nếu project dùng database-first/scaffolding, sau khi database local đã update thì scaffold lại model.

Command cho PowerShell:
```powershell
dotnet ef dbcontext scaffold "Name=ConnectionStrings:DefaultConnection" Npgsql.EntityFrameworkCore.PostgreSQL `
  --project RoadmapPlatform.Infrastructure `
  --startup-project RoadmapPlatform.Api `
  --context ApplicationDbContext `
  --context-dir Data `
  --output-dir Entities `
  --force
```

Command cho CMD/Git Bash:
```bash
dotnet ef dbcontext scaffold "Name=ConnectionStrings:DefaultConnection" Npgsql.EntityFrameworkCore.PostgreSQL \
  --project RoadmapPlatform.Infrastructure \
  --startup-project RoadmapPlatform.Api \
  --context ApplicationDbContext \
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

Sau đó kiểm tra model/entity có đúng với schema mới không.

### Bước 6: Commit thay đổi

Commit nên bao gồm:
```text
Migration file mới
schema.sql đã cập nhật
seed.sql nếu có thay đổi
model/entity scaffold nếu có
service/controller/DTO liên quan nếu có
```

Ví dụ commit:
```bash
git commit -m "feat: add user streak properties"
```

## 6. Pull Request note

Nếu PR có thay đổi database, bắt buộc ghi rõ:
```text
Database changes:
- Added docs/database/migrations/002-user-streak-properties.sql
- Updated schema.sql
- No seed.sql changes
- Scaffolded UserProfile model
```

Nếu có seed:
```text
Database changes:
- Added docs/database/migrations/003-add-rbac-tables.sql
- Updated schema.sql
- Updated seed.sql with default roles and permissions
```

Nếu không có thay đổi database:
```text
Database changes:
- None
```

## 7. Quy tắc quan trọng

- Có thay đổi database thì phải có file trong `migrations/`.
- Sau khi tạo migration, phải cập nhật `schema.sql`.
- Chỉ cập nhật `seed.sql` khi có dữ liệu mặc định cần thêm.
- Không sửa migration cũ nếu file đó đã được merge vào `main`.
- Nếu migration cũ sai, tạo migration mới để sửa.
- Không commit connection string thật.
- Không commit dữ liệu cá nhân thật.
- Nếu scaffold lại model, ghi rõ trong Pull Request.

## 8. Ví dụ workflow hoàn chỉnh

```bash
git switch main
git pull origin main

git checkout -b feature/user-streak

# tạo file:
# docs/database/migrations/002-user-streak-properties.sql

# cập nhật:
# docs/database/schema.sql

# chạy thử migration trên local database
psql -U postgres -d database_name -f docs/database/migrations/002-user-streak-properties.sql

# scaffold lại nếu cần

git add .
git commit -m "feat: add user streak properties"

git push origin feature/user-streak
```

Sau đó tạo Pull Request và ghi rõ phần `Database changes`.

## 9. Tóm tắt

```text
schema.sql = database schema mới nhất
seed.sql = dữ liệu mặc định/mẫu
migrations/ = lịch sử thay đổi database

Có sửa database -> tạo migration file mới
Có migration mới -> cập nhật schema.sql
Có dữ liệu mặc định mới -> cập nhật seed.sql
Có scaffold lại -> ghi rõ trong PR
```
