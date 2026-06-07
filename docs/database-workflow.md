# Database Workflow

Tài liệu này quy định cách team quản lý database thông qua các file trong `docs/database`.

Mục tiêu chính:

- Giữ database schema rõ ràng và nhất quán.
- Quản lý thay đổi database bằng migration file.
- Tách seed data theo nhóm dữ liệu để dễ bảo trì.
- Giúp team reset, seed, scaffold và review database changes theo cùng một workflow.

## Tổng quan workflow

Team quản lý database bằng 3 nhóm file chính:

- `schema.sql`: snapshot schema mới nhất.
- `migrations/`: lịch sử thay đổi schema.
- `seeds/`: dữ liệu mẫu hoặc dữ liệu nền dùng cho dev/test.

Khi có thay đổi database, không chỉ sửa một file riêng lẻ. Cần kiểm tra thay đổi đó ảnh hưởng đến:

- `schema.sql`
- `migrations/`
- `seeds/`
- scaffolded entity/model
- backend service/controller/DTO
- Pull Request note

> [!IMPORTANT]
> Nếu thay đổi cấu trúc database, phải có migration mới và phải cập nhật lại `schema.sql`.

## Cấu trúc thư mục

```text
docs/
└── database/
    ├── schema.sql
    ├── reset-database.sql
    ├── seed.sql
    ├── migrations/
    │   ├── 001-initial-schema.sql
    │   ├── 002-ai-credit-limits.sql
    │   └── 003-update-roadmap-tables.sql
    └── seeds/
        ├── core/
        │   ├── users.seed.sql
        │   ├── roles.seed.sql
        │   └── shared-skills.seed.sql
        ├── roadmaps/
        │   ├── frontend-roadmap.seed.sql
        │   └── backend-roadmap.seed.sql
        └── job-market/
            └── job-categories.seed.sql
```

Cấu trúc này có thể mở rộng theo feature.

Ví dụ:

- `seeds/core/` cho dữ liệu nền dùng chung.
- `seeds/roadmaps/` cho dữ liệu riêng của roadmap feature.
- `seeds/job-market/` cho dữ liệu riêng của job market feature.
- `seeds/<feature>/` cho seed data riêng của feature khác.

## Các loại seed data

Seed data nên được chia theo phạm vi sử dụng.

| Loại seed | Mục đích | Ví dụ |
|---|---|---|
| Core seed | Dữ liệu nền dùng chung cho toàn hệ thống. | roles, demo users, shared skills |
| Feature seed | Dữ liệu chỉ phục vụ một feature cụ thể. | roadmap seed, job market seed, portfolio sample data |
| Demo seed | Dữ liệu để test app trong môi trường dev. | demo users, demo portfolio, demo progress |
| Lookup seed | Dữ liệu danh mục ít thay đổi. | roles, categories, skill list, status options |

## Core seed

Core seed là dữ liệu nền được nhiều phần của hệ thống sử dụng.

Ví dụ:

```text
docs/database/seeds/core/users.seed.sql
docs/database/seeds/core/roles.seed.sql
docs/database/seeds/core/shared-skills.seed.sql
```

Dùng core seed cho dữ liệu như:

- Role mặc định.
- Demo user dùng chung.
- Auth provider của demo user.
- Skill hoặc category dùng bởi nhiều feature.
- Lookup table được nhiều module reference.

> [!IMPORTANT]
> Nếu auth provider chỉ dùng để login cho demo user, nên để chung trong `users.seed.sql` hoặc một file user-related seed trong `seeds/core/`. Không cần tách `seeds/auth/` chỉ để seed auth provider demo.

## Feature-specific seed

Feature-specific seed là dữ liệu chỉ thuộc về một feature hoặc một nhóm chức năng.

Ví dụ:

```text
docs/database/seeds/roadmaps/frontend-roadmap.seed.sql
docs/database/seeds/job-market/job-categories.seed.sql
docs/database/seeds/portfolio/demo-portfolio.seed.sql
```

Dùng feature seed cho dữ liệu như:

- Roadmap mẫu.
- Portfolio mẫu.
- Job market category.
- Resource mẫu.
- Dữ liệu test riêng của một module.

Feature seed có thể phụ thuộc vào core seed.

Ví dụ:

- Roadmap seed có thể phụ thuộc vào shared skills.
- Portfolio seed có thể phụ thuộc vào demo users.
- Progress seed có thể phụ thuộc vào demo users và roadmap data.

> [!NOTE]
> Không cần tạo folder riêng cho mọi table. Tạo folder theo feature hoặc theo nhóm dữ liệu có ý nghĩa với team.

## Vai trò của từng file

| File hoặc thư mục | Vai trò |
|---|---|
| `schema.sql` | Snapshot mới nhất của database schema. |
| `reset-database.sql` | Reset database local, chạy lại schema, và hiện tại cũng chạy seed. |
| `seed.sql` | Runner chính để gọi các seed file nhỏ hơn theo đúng thứ tự. |
| `migrations/` | Lịch sử thay đổi database schema theo từng file migration. |
| `seeds/core/` | Seed data nền dùng chung cho toàn hệ thống. |
| `seeds/<feature>/` | Seed data riêng cho từng feature. |

## `schema.sql`

`schema.sql` chứa cấu trúc database hiện tại.

File này là snapshot mới nhất của database sau khi đã áp dụng toàn bộ migration.

Nội dung nên có:

```text
CREATE EXTENSION
CREATE TABLE
PRIMARY KEY
FOREIGN KEY
INDEX
CONSTRAINT
ENUM/type nếu có
```

Không nên chứa:

- Dữ liệu mẫu lớn.
- Demo user.
- Roadmap seed.
- Portfolio seed.
- Dữ liệu test cho một feature cụ thể.

> [!IMPORTANT]
> `schema.sql` là nguồn chính để setup database mới từ đầu.

## `migrations/`

`migrations/` chứa lịch sử thay đổi database schema.

Mỗi khi có thay đổi cấu trúc database, tạo một migration mới.

Ví dụ:

```text
001-initial-schema.sql
002-ai-credit-limits.sql
003-update-roadmap-tables.sql
004-add-email-verification-fields.sql
```

Migration dùng để update một database đã tồn tại.

Không dùng migration làm nguồn setup chính cho database mới từ đầu. Khi setup mới, dùng `schema.sql`.

> [!WARNING]
> Không sửa migration cũ nếu file đó đã merge vào `main`. Nếu migration cũ sai, tạo migration mới để sửa.

## `seed.sql`

`seed.sql` là seed runner chính.

File này chỉ nên gọi các seed file nhỏ hơn. Không nhét toàn bộ seed data lớn trực tiếp vào `seed.sql`.

Ví dụ:

```sql
\echo 'Seeding core data...'
\i docs/database/seeds/core/roles.seed.sql
\i docs/database/seeds/core/users.seed.sql
\i docs/database/seeds/core/shared-skills.seed.sql

\echo 'Seeding roadmap data...'
\i docs/database/seeds/roadmaps/frontend-roadmap.seed.sql
\i docs/database/seeds/roadmaps/backend-roadmap.seed.sql

\echo 'Seeding job market data...'
\i docs/database/seeds/job-market/job-categories.seed.sql
```

Thứ tự seed nên đi từ dữ liệu nền đến dữ liệu phụ thuộc:

```text
core seed
feature seed
demo/test seed
```

Lý do:

- Feature seed có thể cần role, user, category hoặc skill đã tồn tại.
- Demo seed có thể cần dữ liệu từ nhiều feature.
- Thứ tự sai dễ gây `foreign key violation`.

> [!IMPORTANT]
> `seed.sql` chỉ là runner. Seed data thật nên nằm trong `seeds/`.

## `reset-database.sql`

`reset-database.sql` dùng để reset database local trong môi trường dev.

Hiện tại file này làm 4 việc:

- Drop schema hiện tại.
- Tạo lại schema `public`.
- Chạy lại `schema.sql`.
- Chạy lại `seed.sql`.

Ví dụ nội dung file:

```sql
DROP SCHEMA public CASCADE;
CREATE SCHEMA public;

\i docs/database/schema.sql
\i docs/database/seed.sql
```

Điều này phù hợp cho local development vì sau khi reset, database có sẵn dữ liệu mẫu để test app.

> [!WARNING]
> Vì file này cũng chạy seed, reset local database sẽ xóa dữ liệu cũ và load lại dữ liệu mẫu từ đầu.

## Quy tắc đặt tên migration file

Format:

```text
<number>-<short-description>.sql
```

Ví dụ:

```text
001-initial-schema.sql
002-ai-credit-limits.sql
003-update-roadmap-tables.sql
004-add-email-verification-fields.sql
```

Quy tắc:

- Số thứ tự tăng dần.
- Dùng 3 chữ số: `001`, `002`, `003`.
- Tên file viết thường.
- Dùng dấu `-` thay vì khoảng trắng.
- Tên file phải mô tả rõ thay đổi.

Không nên đặt tên như:

```text
002-update.sql
003-fix.sql
004-new-changes.sql
```

## Quy tắc đặt tên seed file

Format:

```text
<feature-or-dataset>.seed.sql
```

Ví dụ:

```text
users.seed.sql
roles.seed.sql
shared-skills.seed.sql
frontend-roadmap.seed.sql
job-categories.seed.sql
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
copy.sql
new-fixed-seed.sql
```

## Khi nào cần tạo migration file?

Tạo file mới trong `migrations/` khi có thay đổi database schema.

Các trường hợp thường gặp:

- Thêm table mới.
- Thêm column mới.
- Sửa kiểu dữ liệu column.
- Đổi tên column hoặc table.
- Thêm foreign key.
- Thêm index.
- Thêm constraint.
- Thêm enum hoặc type mới.
- Thay đổi quan hệ giữa các table.

Ví dụ:

```text
Thêm current_streak vào user_profile
Thêm deleted_at vào user
Thêm bảng resource
Thêm bảng roadmap_node
Thêm status cho roadmap_version
```

Không cần tạo migration khi chỉ thêm hoặc sửa seed data.

> [!NOTE]
> Dữ liệu mẫu nên nằm trong `seeds/`, không nằm trong migration, trừ khi đó là dữ liệu bắt buộc để schema hoạt động đúng.

## Workflow khi thay đổi database schema

### Bước 1: Tạo migration file mới

Ví dụ muốn thêm streak properties cho user:

```text
docs/database/migrations/004-user-streak-properties.sql
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

Sau khi viết migration, cập nhật lại `schema.sql` để phản ánh database schema mới nhất.

Ví dụ nếu migration thêm các column sau:

```sql
current_streak integer NOT NULL DEFAULT 0,
longest_streak integer NOT NULL DEFAULT 0,
last_interaction timestamptz NULL,
```

thì trong `schema.sql` cũng phải có các column này.

> [!IMPORTANT]
> Có migration mới thì phải cập nhật `schema.sql`.

### Bước 3: Cập nhật seed nếu cần

Nếu thay đổi schema cần dữ liệu mặc định hoặc dữ liệu mẫu, cập nhật seed file phù hợp.

Ví dụ:

```text
Demo user và auth provider demo -> seeds/core/users.seed.sql
Shared skill -> seeds/core/shared-skills.seed.sql
Roadmap mẫu -> seeds/roadmaps/<roadmap-name>.seed.sql
Feature khác -> seeds/<feature>/<dataset>.seed.sql
```

Không đưa toàn bộ seed data vào `seed.sql`.

### Bước 4: Cập nhật backend code nếu cần

Nếu schema thay đổi ảnh hưởng đến API hoặc business logic, cập nhật các phần liên quan.

Ví dụ:

- Entity/model scaffold.
- DTO.
- Service.
- Controller.
- Validation.
- API documentation.

### Bước 5: Kiểm tra local database

Sau khi cập nhật file, chạy script phù hợp ở phần `Cách chạy database scripts`.

Tùy trường hợp:

- Chạy migration nếu đang update database hiện có.
- Chạy reset nếu muốn rebuild local database từ đầu.
- Chạy seed nếu chỉ thay đổi seed data.

### Bước 6: Scaffold lại model nếu cần

Nếu project dùng database-first hoặc scaffolding, sau khi database local đã update thì scaffold lại model.

Sau khi scaffold, kiểm tra entity/model có khớp với schema mới không.

> [!NOTE]
> Nếu scaffold không đọc được user secrets, có thể tạm dùng connection string local. Không commit connection string thật vào Git.

### Bước 7: Commit thay đổi

Commit nên bao gồm các file liên quan:

- Migration file mới.
- `schema.sql` đã cập nhật.
- Seed file nếu có thay đổi.
- Entity/model scaffold nếu có.
- Service, controller, hoặc DTO liên quan nếu có.

Ví dụ commit:

```bash
git commit -m "feat: update database schema"
```

## Workflow khi thêm hoặc sửa seed data

### Bước 1: Xác định loại seed

Trước khi tạo hoặc sửa seed, xác định dữ liệu thuộc loại nào.

```text
Dữ liệu dùng chung -> seeds/core/
Dữ liệu riêng một feature -> seeds/<feature>/
Dữ liệu demo/test -> đặt trong core hoặc feature phù hợp
```

Ví dụ:

```text
Role mặc định -> seeds/core/roles.seed.sql
Demo user -> seeds/core/users.seed.sql
Auth provider demo cho demo user -> seeds/core/users.seed.sql
Shared skill -> seeds/core/shared-skills.seed.sql
Roadmap frontend -> seeds/roadmaps/frontend-roadmap.seed.sql
Job category -> seeds/job-market/job-categories.seed.sql
```

### Bước 2: Cập nhật `seed.sql` nếu có file mới

Nếu tạo seed file mới, thêm nó vào `seed.sql`.

Đặt đúng thứ tự để tránh lỗi dependency.

Ví dụ thứ tự hợp lý:

```text
roles.seed.sql
users.seed.sql
shared-skills.seed.sql
feature-specific seed files
```

### Bước 3: Seed phải idempotent

Seed nên chạy được nhiều lần mà không tạo duplicate.

Ưu tiên dùng stable key như:

- `slug`
- `email`
- `name` nếu thật sự unique
- external id nếu có

Ví dụ dùng `ON CONFLICT`:

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

Ví dụ tránh overwrite dữ liệu dùng chung:

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

> [!IMPORTANT]
> Seed lớn nên idempotent nếu có thể. Điều này giúp team chạy lại seed nhiều lần mà không bị duplicate data.

### Bước 4: Kiểm tra seed local

Sau khi sửa seed, chạy script phù hợp ở phần `Cách chạy database scripts`.

Tùy trường hợp:

- Chạy `seed.sql` nếu database đã có schema đúng.
- Chạy `reset-database.sql` nếu muốn rebuild database và seed lại từ đầu.


## `psql` setup trên Windows

Kiểm tra `psql`:

```bash
psql --version
```

Nếu không nhận lệnh, thêm PostgreSQL `bin` folder vào PATH.

Ví dụ:

```text
C:\Program Files\PostgreSQL\18\bin
```

> [!WARNING]
> Các lệnh như `\i` và `\echo` là lệnh của `psql`, không phải SQL chuẩn. Không chạy các file có lệnh này trong pgAdmin query editor.

## Cách chạy database scripts

> [!NOTE]
> Chạy các lệnh dưới đây từ project root, tức là thư mục gốc của project nơi có thư mục `.git`.

### Chạy schema từ đầu

Dùng khi muốn tạo schema trên database trống.

```bash
psql -U postgres -d database_name -f docs/database/schema.sql
```

Nếu cần khai báo host và port rõ ràng:

```bash
psql -h localhost -p 5432 -U postgres -d database_name -f docs/database/schema.sql
```

### Chạy migration

Dùng khi muốn update một database đã tồn tại.

```bash
psql -U postgres -d database_name -f docs/database/migrations/004-user-streak-properties.sql
```

Nếu cần khai báo host và port rõ ràng:

```bash
psql -h localhost -p 5432 -U postgres -d database_name -f docs/database/migrations/004-user-streak-properties.sql
```

### Chạy seed

Dùng khi database đã có schema đúng và chỉ cần load seed data.

```bash
psql -U postgres -d database_name -f docs/database/seed.sql
```

Nếu cần khai báo host và port rõ ràng:

```bash
psql -h localhost -p 5432 -U postgres -d database_name -f docs/database/seed.sql
```

### Reset database local

Vì `reset-database.sql` hiện tại cũng chạy seed, lệnh này sẽ reset schema và load lại dữ liệu mẫu.

```bash
psql -U postgres -d database_name -f docs/database/reset-database.sql
```

Nếu cần khai báo host và port rõ ràng:

```bash
psql -h localhost -p 5432 -U postgres -d database_name -f docs/database/reset-database.sql
```

### Scaffold lại model

Dùng khi project dùng database-first/scaffolding và schema đã thay đổi.

PowerShell:

```powershell
dotnet ef dbcontext scaffold "Name=ConnectionStrings:DefaultConnection" Npgsql.EntityFrameworkCore.PostgreSQL `
  --project src/backend/RoadmapPlatform.Infrastructure `
  --startup-project src/backend/RoadmapPlatform.Api `
  --context ApplicationDbContext `
  --context-dir Data `
  --output-dir Entities `
  --force `
  --no-onconfiguring
```

CMD hoặc Git Bash:

```bash
dotnet ef dbcontext scaffold "Name=ConnectionStrings:DefaultConnection" Npgsql.EntityFrameworkCore.PostgreSQL \
  --project src/backend/RoadmapPlatform.Infrastructure \
  --startup-project src/backend/RoadmapPlatform.Api \
  --context ApplicationDbContext \
  --context-dir Data \
  --output-dir Entities \
  --force \
  --no-onconfiguring
```

> [!NOTE]
> `"Name=ConnectionStrings:DefaultConnection"` cho phép EF đọc connection string từ user-secrets hoặc `appsettings`.

## Pull Request note

Nếu PR có thay đổi database, bắt buộc ghi rõ phần `Database changes`.

Ví dụ khi có migration:

```text
Database changes:
- Added docs/database/migrations/004-user-streak-properties.sql
- Updated docs/database/schema.sql
- No seed changes
- Scaffolded UserProfile entity
```

Ví dụ khi có seed:

```text
Database changes:
- Updated docs/database/seeds/core/users.seed.sql
- Updated docs/database/seeds/core/shared-skills.seed.sql
- Updated docs/database/seeds/roadmaps/backend-roadmap.seed.sql
- Updated docs/database/seed.sql
```

Ví dụ khi không có thay đổi database:

```text
Database changes:
- None
```

> [!IMPORTANT]
> Nếu scaffold lại model, ghi rõ trong Pull Request.

## Quy tắc quan trọng

- Có thay đổi database schema thì phải có file trong `migrations/`.
- Sau khi tạo migration, phải cập nhật `schema.sql`.
- `schema.sql` luôn phản ánh database mới nhất.
- `seed.sql` chỉ là runner, không chứa seed data lớn.
- Seed data dùng chung nên nằm trong `seeds/core/`.
- Seed data riêng feature nên nằm trong `seeds/<feature>/`.
- Auth provider demo nên đi cùng demo user seed nếu nó chỉ phục vụ demo login.
- Seed phải idempotent nếu có thể.
- Core seed phải chạy trước feature seed.
- Không sửa migration cũ nếu file đó đã merge vào `main`.
- Nếu migration cũ sai, tạo migration mới để sửa.
- Không commit connection string thật.
- Không commit dữ liệu cá nhân thật.
- Không commit password thật hoặc secret.
- Nếu scaffold lại model, ghi rõ trong Pull Request.

## Ví dụ workflow hoàn chỉnh

```bash
git switch main
git pull origin main

git checkout -b feature/update-database-schema

# Tạo migration:
# docs/database/migrations/004-user-streak-properties.sql

# Cập nhật:
# docs/database/schema.sql
# docs/database/seed.sql nếu runner thay đổi
# docs/database/seeds/<folder>/<file>.seed.sql nếu có seed mới

# Kiểm tra local database bằng script phù hợp
# Xem phần: Cách chạy database scripts

# Scaffold lại nếu cần

git add .
git commit -m "feat: update database schema"

git push origin feature/update-database-schema
```

Sau đó tạo Pull Request và ghi rõ phần `Database changes`.

## Tóm tắt

Các điểm quan trọng nhất:

- `schema.sql` là snapshot schema mới nhất.
- `migrations/` lưu lịch sử thay đổi schema.
- `seed.sql` chỉ là seed runner.
- `seeds/core/` chứa seed data dùng chung.
- `seeds/<feature>/` chứa seed data riêng cho từng feature.
- Auth provider demo nên đi cùng demo user seed nếu nó phụ thuộc trực tiếp vào demo user.
- `reset-database.sql` hiện tại reset schema và chạy seed lại.
- `psql` setup nằm trước phần chạy script.
- Tất cả command chạy database script nằm trong một section riêng.
- Có sửa database schema thì tạo migration file mới.
- Có migration mới thì cập nhật `schema.sql`.
- Có seed data mới thì cập nhật đúng seed file và update `seed.sql` nếu cần.
- Core seed chạy trước feature seed.
- Seed nên idempotent để team có thể chạy lại nhiều lần.
- Có scaffold lại thì ghi rõ trong PR.
