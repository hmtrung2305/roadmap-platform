# Git Workflow
*Brought to you by ChatGPT with some light editing*

Tài liệu này quy định cách đặt tên branch, cách viết commit và workflow làm việc với Git cho backend/frontend project. 
## 1. Branch chính

### `main`

Branch chính của project.
- Chỉ chứa code **ổn định**.
- KHÔNG commit trực tiếp lên `main`.
- Code chỉ được merge vào `main` thông qua Pull Request.

## 2. Quy tắc đặt tên branch

Format chung:
```text
<type>/<short-description>
```

Ví dụ:
```text
feature/login-api
feature/email-verification
feature/portfolio-page

fix/login-error-handling
fix/avatar-upload-bug

refactor/auth-service
refactor/project-structure

docs/backend-structure
docs/git-workflow
```

Các loại branch thường dùng:
```text
feature/   Thêm chức năng mới
fix/       Sửa lỗi
refactor/  Sửa code nhưng không đổi behavior
docs/      Viết hoặc sửa tài liệu
chore/     Việc phụ như config, package, cleanup
test/      Thêm hoặc sửa test
```

Tên branch nên:
- Viết thường.
- Dùng dấu `-` thay vì khoảng trắng.
- Ngắn gọn, dễ hiểu.
- Không đặt tên quá chung như `fix/error`, `feature/update`, `test/code`.

## 3. Quy tắc viết commit

Team dùng format đơn giản theo kiểu Conventional Commits:
```text
<type>: <short message>
```

Ví dụ:
```text
feat: add login endpoint
feat: add email verification OTP
fix: handle deleted account login
fix: prevent duplicate username
refactor: split auth service logic
docs: add backend project structure
chore: update appsettings example
test: add auth service tests
```

Các type thường dùng:
```text
feat      Thêm chức năng mới
fix       Sửa lỗi
refactor  Sửa code không làm đổi chức năng
docs      Sửa tài liệu
chore     Việc phụ như config, dependency, cleanup
test      Thêm hoặc sửa test
style     Sửa format code, không đổi logic
```

Commit message nên:
- Ngắn gọn.
- Dùng tiếng Anh, tốt nhất nên ở thì hiện tại. Ví dụ: thay vì `implemented oauth login` thì là `implement oauth login`
- Nói rõ thay đổi chính.
- Không viết quá chung như `update code`, `fix bug`, `change stuff`.

## 4. Workflow làm việc đơn giản

### Bước 1: Cập nhật code mới nhất

```bash
git switch main
git pull origin main
```

### Bước 2: Tạo branch mới

```bash
git switch -c feature/login-api
```

### Bước 3: Code và commit

```bash
git add .
git commit -m "feat: add login endpoint"
```

Nên commit theo từng phần nhỏ, không gom quá nhiều thay đổi không liên quan vào một commit.

### Bước 4: Push branch lên remote

```bash
git push origin feature/login-api
```

### Bước 5: Tạo Pull Request

Tạo Pull Request từ branch của mình vào `main` hoặc `develop`.

Pull Request nên có:
- Mô tả ngắn gọn thay đổi.
- Những endpoint/page/module bị ảnh hưởng.
- Ghi chú nếu có thay đổi database. Để rõ hơn về workflow với database thì qua bên `database-workflow.md`).
- Screenshot nếu có thay đổi UI.

### Bước 6: Review và merge

Trước khi merge:
- Code chạy được.
- Không có lỗi build.
- Không push file nhạy cảm như `.env`, secret key, connection string thật.
- Không merge code đang làm dở.
- Đã resolve conflict nếu có.

## 5. Quy tắc khi làm việc nhóm
- Không commit trực tiếp lên `main`.
- Mỗi task nên có một branch riêng.
- Pull code mới nhất trước khi bắt đầu làm.
- Commit nhỏ, rõ ràng.
- Pull Request không nên quá lớn.
- Nếu sửa database, cần ghi rõ trong Pull Request.
- Nếu đổi API response/request, cần báo cho frontend/backend liên quan.
- Nếu có conflict, người tạo Pull Request nên tự resolve trước.


