# Database Workflow

Tài liệu này quy định cách team quản lý database thông qua các file trong `docs/database`.

## 1. Cấu trúc thư mục database docs

```text
docs/
└── database/
    ├── schema.sql
    ├── reset-database.sql
    ├── seed.sql
    ├── migrations/
    │   ├── 001-ai-credit-limits.sql
    │   └── 002-update-roadmap-tables.sql
    └── seeds/
        ├── core/
        │   ├── users.seed.sql
        │   └── shared-skills.seed.sql
        └── roadmaps/
            ├── ai-engineer-roadmap.seed.sql
            ├── backend-roadmap.seed.sql
            ├── data-engineer-roadmap.seed.sql
            ├── frontend-roadmap.seed.sql
            ├── game-developer-roadmap.seed.sql
            └── network-engineer-roadmap.seed.sql
```

## 2. Vai trò của từng file

### `schema.sql`

Chứa cấu trúc database hiện tại.

File này là snapshot mới nhất của database sau khi đã áp dụng toàn bộ migration.

Bao gồm:

```text
CREATE EXTENSION
CREATE TABLE
PRIMARY KEY
FOREIGN KEY
INDEX
CONSTRAINT
ENUM/type nếu có
```

Không đưa dữ liệu mẫu hoặc dữ liệu mặc định vào `schema.sql`.

### `reset-database.sql`

Dùng để reset database local trong môi trường dev.

File này thường làm 3 việc:

```text
Drop schema hiện tại
Tạo lại schema public
Chạy schema.sql
```

Ví dụ:

```sql
DROP SCHEMA public CASCADE;
CREATE SCHEMA public;

\i docs/database/schema.sql
```

Nếu muốn reset và seed lại toàn bộ dữ liệu mẫu, có thể thêm:

```sql
\i docs/database/seed.sql
```

Lưu ý: `\i` là lệnh của `psql`, không phải SQL chuẩn. Nên chạy file này bằng `psql`, không chạy trong pgAdmin query editor.

### `seed.sql`

Là file runner chính cho seed data.

File này không nên chứa toàn bộ dữ liệu seed lớn. Nó chỉ nên gọi các file seed nhỏ hơn.

Ví dụ:

```sql
\echo 'Seeding core data...'
\i docs/database/seeds/core/users.seed.sql
\i docs/database/seeds/core/shared-skills.seed.sql

\echo 'Seeding roadmaps...'
\i docs/database/seeds/roadmaps/frontend-roadmap.seed.sql
\i docs/database/seeds/roadmaps/backend-roadmap.seed.sql
\i docs/database/seeds/roadmaps/data-engineer-roadmap.seed.sql
\i docs/database/seeds/roadmaps/ai-engineer-roadmap.seed.sql
\i docs/database/seeds/roadmaps/game-developer-roadmap.seed.sql
\i docs/database/seeds/roadmaps/network-engineer-roadmap.seed.sql
```

Order quan trọng:

```text
core seed trước
roadmap seed sau
```

Vì roadmap seed có thể phụ thuộc vào users, shared skills hoặc dữ liệu nền khác.

### `migrations/`

Chứa lịch sử thay đổi database theo từng file nhỏ.

Mỗi khi có thay đổi database, tạo một migration mới để ghi lại thay đổi đó.

Ví dụ:

```text
001-initial-schema.sql
002-update-roadmap-tables-v3.sql
003-add-email-verification-fields.sql
004-add-deleted-at-to-users.sql
```

Migration dùng để update một database đã tồn tại.

Không dùng migration làm nguồn schema chính khi setup database mới từ đầu. Khi setup mới, dùng `schema.sql`.

### `seeds/core/`

Chứa seed data dùng chung cho toàn hệ thống.

Ví dụ:

```text
users.seed.sql
shared-skills.seed.sql
roles.seed.sql
admin-users.seed.sql
demo-auth-providers.seed.sql
```


## 3. Quy tắc đặt tên migration file

Format:

```text
<number>-<short-description>.sql
```

Ví dụ:

```text
001-initial-schema.sql
002-update-roadmap-tables-v3.sql
003-add-email-verification-fields.sql
004-add-deleted-at-to-users.sql
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

## 4. Quy tắc đặt tên seed file

Format:

```text
<feature-or-dataset>.seed.sql
```

Ví dụ:

```text
users.seed.sql
shared-skills.seed.sql
frontend-roadmap.seed.sql
backend-roadmap.seed.sql
data-engineer-roadmap.seed.sql
```

Quy tắc:

- Tên file viết thường.
- Dùng dấu `-`.
- Kết thúc bằng `.seed.sql`.
- Mỗi file chỉ nên seed một nhóm dữ liệu rõ ràng.

Không nên đặt tên như:

```text
seed1.sql
final-seed.sql
roadmap-copy.sql
new-fixed-seed.sql
```

## 5. Khi nào cần tạo migration file?

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
Thay đổi quan hệ giữa các table
```

Ví dụ:

```text
Thêm streak_count vào user_profile
Thêm deleted_at vào user
Thêm bảng resource
Thêm bảng resource_chunk
Thêm bảng roadmap_node
Thêm status cho roadmap_version
```

Không tạo migration chỉ vì thêm dữ liệu mẫu lớn. Dữ liệu mẫu nên nằm trong `seeds/`.

## 6. Workflow khi thay đổi database schema

### Bước 1: Tạo migration file mới

Ví dụ muốn thêm streak properties cho user:

```text
docs/database/migrations/003-user-streak-properties.sql
```

Nội dung ví dụ:

```sql
ALTER TABLE public.user_profile
ADD COLUMN current_streak integer NOT NULL DEFAULT 0;

ALTER TABLE public.user_profile
ADD COLUMN longest_streak integer NOT NULL DEFAULT 0;

ALTER TABLE public.user_profile
ADD COLUMN last_interaction timestamptz NULL;
```

### Bước 2: Cập nhật `schema.sql`

Sau khi viết migration, cập nhật lại `schema.sql` để phản ánh cấu trúc database mới nhất.

Ví dụ nếu migration thêm column:

```sql
current_streak integer NOT NULL DEFAULT 0,
longest_streak integer NOT NULL DEFAULT 0,
last_interaction timestamptz NULL,
```

thì trong `schema.sql` cũng phải có các column này.

### Bước 3: Cập nhật seed nếu cần

Nếu thay đổi cần dữ liệu mặc định, cập nhật file seed phù hợp.

Ví dụ:

```text
Dữ liệu user demo -> seeds/core/users.seed.sql
Skill dùng chung -> seeds/core/shared-skills.seed.sql
Roadmap frontend -> seeds/roadmaps/frontend-roadmap.seed.sql
```

Không nhét tất cả vào `seed.sql`. `seed.sql` chỉ nên là runner.

### Bước 4: Chạy migration trên local database

Chạy migration trên database local để kiểm tra.

Ví dụ:

```bash
psql -U postgres -d database_name -f docs/database/migrations/003-user-streak-properties.sql
```

Hoặc nếu cần host/port rõ ràng:

```bash
psql -h localhost -p 5432 -U postgres -d database_name -f docs/database/migrations/003-user-streak-properties.sql
```

Có thể copy script vào pgAdmin để chạy nếu migration chỉ chứa SQL chuẩn.

Không dùng pgAdmin cho file có `\i` hoặc `\echo`, vì đó là lệnh riêng của `psql`.

### Bước 5: Scaffold lại model nếu cần

Nếu project dùng database-first/scaffolding, sau khi database local đã update thì scaffold lại model.

PowerShell:

```powershell
dotnet ef dbcontext scaffold "Name=ConnectionStrings:DefaultConnection" Npgsql.EntityFrameworkCore.PostgreSQL `
  --project src/backend/RoadmapPlatform.Infrastructure `
  --startup-project src/backend/RoadmapPlatform.Api `
  --context ApplicationDbContext `
  --context-dir Data `
  --output-dir Entities `
  --force
```

CMD/Git Bash:

```bash
dotnet ef dbcontext scaffold "Name=ConnectionStrings:DefaultConnection" Npgsql.EntityFrameworkCore.PostgreSQL \
  --project src/backend/RoadmapPlatform.Infrastructure \
  --startup-project src/backend/RoadmapPlatform.Api \
  --context ApplicationDbContext \
  --context-dir Data \
  --output-dir Entities \
  --force
```

Note:

```text
"Name=ConnectionStrings:DefaultConnection" cho phép EF đọc connection string từ user-secrets/appsettings.
Nếu scaffold không đọc được user secrets, tạm thời dùng connection string cứng local.
Không commit connection string cứng vào Git.
```

Sau đó kiểm tra entity/model có đúng với schema mới không.

### Bước 6: Commit thay đổi

Commit nên bao gồm:

```text
Migration file mới
schema.sql đã cập nhật
seed file nếu có thay đổi
entity/model scaffold nếu có
service/controller/DTO liên quan nếu có
```

Ví dụ commit:

```bash
git commit -m "feat: update roadmap database schema"
```

## 7. Workflow khi thêm hoặc sửa seed data

### Bước 1: Chọn đúng seed file

```text
User/demo account -> seeds/core/users.seed.sql
Shared skills -> seeds/core/shared-skills.seed.sql
Roadmap cụ thể -> seeds/roadmaps/<roadmap-name>.seed.sql
```

### Bước 2: Seed phải idempotent

Seed nên chạy được nhiều lần mà không tạo duplicate.

Ưu tiên dùng stable slug và `ON CONFLICT`.

Ví dụ:

```sql
INSERT INTO public.skill (slug, name, category, description, is_active)
VALUES
('python', 'Python', 'Programming', 'General-purpose programming language used across backend, data, AI, automation, and tooling.', true)
ON CONFLICT (slug) DO UPDATE SET
    name = EXCLUDED.name,
    category = EXCLUDED.category,
    description = EXCLUDED.description,
    is_active = true;
```

Với roadmap-specific skill, tránh overwrite shared skill metadata:

```sql
INSERT INTO public.skill (name, slug, category, description, is_active)
SELECT ss.name, ss.slug, ss.category, ss.description, true
FROM seed_skill ss
WHERE NOT EXISTS (
    SELECT 1
    FROM public.skill s
    WHERE s.slug = ss.slug OR s.name = ss.name
);
```

### Bước 3: Chạy seed runner

Từ project root:

```powershell
psql -U postgres -d database_name -f docs/database/seed.sql
```

Hoặc:

```powershell
psql -h localhost -p 5432 -U postgres -d database_name -f docs/database/seed.sql
```

### Bước 4: Nếu seed fail

Kiểm tra các lỗi thường gặp:

```text
syntax error near ON
duplicate key value violates unique constraint
relation does not exist
invalid input syntax for type uuid
foreign key violation
```

Cách xử lý nhanh:

```text
syntax error near ON -> kiểm tra dấu ; trước ON CONFLICT
duplicate key -> kiểm tra slug/name bị duplicate
relation does not exist -> chạy schema hoặc migration trước
foreign key violation -> kiểm tra order seed file
```

## 8. Cách chạy database scripts

### Chạy schema từ đầu

```bash
psql -U postgres -d database_name -f docs/database/schema.sql
```

### Chạy migrations

```bash
psql -U postgres -d database_name -f docs/database/migrations/003-user-streak-properties.sql
```

### Chạy seed

```bash
psql -U postgres -d database_name -f docs/database/seed.sql
```

### Reset database local

```bash
psql -U postgres -d database_name -f docs/database/reset-database.sql
```

## 9. `psql` setup trên Windows

Kiểm tra `psql`:

```bash
psql --version
```

Nếu không nhận lệnh, thêm PostgreSQL `bin` folder vào PATH.

Ví dụ:

```text
C:\Program Files\PostgreSQL\18\bin
```

Không thêm đường dẫn tới file `.exe`.

Sai:

```text
C:\Program Files\PostgreSQL\18\bin\psql.exe
```

Đúng:

```text
C:\Program Files\PostgreSQL\18\bin
```

## 10. Pull Request note

Nếu PR có thay đổi database, bắt buộc ghi rõ:

```text
Database changes:
- Added docs/database/migrations/003-user-streak-properties.sql
- Updated docs/database/schema.sql
- No seed changes
- Scaffolded UserProfile entity
```

Nếu có seed:

```text
Database changes:
- Updated docs/database/seeds/core/shared-skills.seed.sql
- Updated docs/database/seeds/roadmaps/backend-roadmap.seed.sql
- Updated docs/database/seed.sql
```

Nếu không có thay đổi database:

```text
Database changes:
- None
```

## 11. Quy tắc quan trọng

- Có thay đổi database schema thì phải có file trong `migrations/`.
- Sau khi tạo migration, phải cập nhật `schema.sql`.
- `schema.sql` luôn phản ánh database mới nhất.
- `seed.sql` chỉ là runner, không nên chứa seed data lớn.
- Roadmap seed lớn phải nằm trong `seeds/roadmaps/`.
- Shared skills phải nằm trong `seeds/core/shared-skills.seed.sql`.
- Seed phải idempotent nếu có thể.
- Không sửa migration cũ nếu file đó đã merge vào `main`.
- Nếu migration cũ sai, tạo migration mới để sửa.
- Không commit connection string thật.
- Không commit dữ liệu cá nhân thật.
- Không commit password thật hoặc secret.
- Nếu scaffold lại model, ghi rõ trong Pull Request.

## 12. Ví dụ workflow hoàn chỉnh

```bash
git switch main
git pull origin main

git checkout -b feature/update-roadmap-schema

# tạo migration:
# docs/database/migrations/002-update-roadmap-tables-v3.sql

# cập nhật:
# docs/database/schema.sql
# docs/database/seed.sql nếu runner thay đổi
# docs/database/seeds/core/shared-skills.seed.sql nếu có shared skills mới
# docs/database/seeds/roadmaps/<roadmap>.seed.sql nếu có roadmap seed mới

# chạy thử migration trên local database
psql -U postgres -d database_name -f docs/database/migrations/002-update-roadmap-tables-v3.sql

# chạy seed nếu cần
psql -U postgres -d database_name -f docs/database/seed.sql

# scaffold lại nếu cần

git add .
git commit -m "feat: update roadmap database schema"

git push origin feature/update-roadmap-schema
```

Sau đó tạo Pull Request và ghi rõ phần `Database changes`.

## 13. Tóm tắt

```text
schema.sql = database schema mới nhất
reset-database.sql = reset local database
seed.sql = seed runner
migrations/ = lịch sử thay đổi database
seeds/core/ = dữ liệu seed dùng chung
seeds/roadmaps/ = seed riêng từng roadmap

Có sửa database schema -> tạo migration file mới
Có migration mới -> cập nhật schema.sql
Có seed data mới -> cập nhật seed file phù hợp
Có roadmap seed lớn -> để trong seeds/roadmaps/
Có shared skill -> để trong seeds/core/shared-skills.seed.sql
Có scaffold lại -> ghi rõ trong PR
```
