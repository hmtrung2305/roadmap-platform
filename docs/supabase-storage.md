# Supabase Storage cho tài liệu upload

Project hỗ trợ hai cách lưu file tài liệu:

- `Local`: dùng cho local dev, lưu file vào `wwwroot/docs`.
- `Supabase`: dùng cho production trên Render, lưu file vào Supabase Storage.

## Local

Mặc định trong `appsettings.json`:

```json
{
  "FileStorage": {
    "Provider": "Local",
    "LocalFolder": "docs"
  }
}
```

Khi chạy local, không cần thêm config. File upload sẽ nằm ở:

```text
src/backend/RoadmapPlatform.Api/wwwroot/docs
```

## Supabase

Trên Supabase, tạo bucket:

```text
roadmap-docs
```

Nên để bucket ở chế độ private. Backend sẽ dùng `service_role key` để upload/read/delete file.

Trên Render backend, thêm environment variables:

```text
FileStorage__Provider=Supabase
SupabaseStorage__Url=https://YOUR_PROJECT_REF.supabase.co
SupabaseStorage__ServiceRoleKey=YOUR_SUPABASE_SERVICE_ROLE_KEY
SupabaseStorage__Bucket=roadmap-docs
```

Không đưa `SupabaseStorage__ServiceRoleKey` vào Vercel frontend và không commit vào Git.

## Luồng hoạt động

Upload:

```text
React frontend
  -> POST /api/resources/upload
  -> ASP.NET backend
  -> Local wwwroot/docs hoặc Supabase Storage
  -> resources.metadata lưu thông tin storage
```

Đọc tài liệu:

```text
React frontend
  -> GET /api/resources/{resourceId}/content
  -> ASP.NET backend đọc file từ Local hoặc Supabase Storage
  -> trả markdown content
```

Frontend không cần biết file đang nằm ở local hay Supabase.

## Lưu ý

Các file upload cũ đang nằm trong `wwwroot/docs` sẽ không tự chuyển sang Supabase. Sau khi bật `FileStorage__Provider=Supabase`, chỉ các file upload mới sẽ đi vào Supabase Storage.
