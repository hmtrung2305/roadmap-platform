# Database Scripts

Thư mục này chứa các script dùng để chạy database PostgreSQL cho môi trường local.

## Yêu cầu trước khi chạy

Trước khi chạy các file `.bat`, cần đảm bảo:

* Đã cài PostgreSQL.
* Lệnh `psql` chạy được trong terminal.
* Database local đã được tạo sẵn.
* Tên database mặc định là:

```bat
roadmap_platform
```

Database cũng cần hỗ trợ các extension được dùng trong schema:

* `pgcrypto`
* `vector`

Nếu thiếu `vector`, cần cài `pgvector` cho PostgreSQL trước.

## Các file `.bat`

### `apply-schema.bat`

Chạy `schema.sql` để tạo cấu trúc database.

```bat
apply-schema.bat
```

Hoặc chỉ định database:

```bat
apply-schema.bat roadmap_platform
```

### `seed-database.bat`

Chạy `seed.sql` để thêm dữ liệu mẫu/default.

```bat
seed-database.bat
```

Hoặc chỉ định database:

```bat
seed-database.bat roadmap_platform
```

### `reset-database.bat`

Chạy `reset-database.sql` để reset lại schema `public`.

```bat
reset-database.bat
```

Hoặc chỉ định database:

```bat
reset-database.bat roadmap_platform
```

Cảnh báo: file này sẽ xóa toàn bộ dữ liệu local trong schema `public`.

### `reset-and-seed-database.bat`

Chạy toàn bộ quy trình reset database local:

1. Reset schema `public`
2. Chạy `schema.sql`
3. Chạy `seed.sql`

```bat
reset-and-seed-database.bat
```

Hoặc chỉ định database:

```bat
reset-and-seed-database.bat roadmap_platform
```

Cảnh báo: file này sẽ xóa dữ liệu local hiện tại rồi tạo lại schema và seed data.

## Quy trình thường dùng

Reset và tạo lại database local từ đầu:

```bat
reset-and-seed-database.bat
```

Chạy từng bước riêng để debug:

```bat
reset-database.bat
apply-schema.bat
seed-database.bat
```

## Ghi chú

Các script reset chỉ dùng cho môi trường local/dev.

Không chạy `reset-database.bat` hoặc `reset-and-seed-database.bat` trên production hoặc database dùng chung.
