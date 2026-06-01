# Project Structure

## Backend

Backend sẽ được tổ chức theo kiểu nhiều project trong 1 solution chung
```text
src/
├── UserManagementDemo.Api/
├── UserManagementDemo.Application/
└── UserManagementDemo.Infrastructure/
```
Cái này là để phân ra các tầng giúp quản lý hơn. Lúc e làm cái phần demo cho authentication với portfolio thì thấy đã khá nhiều rồi, nên là sẽ khó quản lý khi mà thêm các phần khác vô.

## Vai trò của từng project nhỏ
### `UserManagementDemo.Api`

```text
UserManagementDemo.Api/  
│ ├── Controllers/  
│ ├── Middleware/  
│ ├── Extensions/  
│ ├── Program.cs  
│ └── appsettings.json  
```

Project này sẽ nhận request từ client rồi gọi service phù hợp và trả về response cho frontend.
Trong đây thì không nên đặt những cái liên quan đến business rule. Chỉ đơn giản là setup (trong `Program.cs`) và điều hướng bằng controller.

**Note**:
`Extensions` là những cái lớp chứa các hàm mở rộng để tách các phần config ra làm cho `Program.cs` nhìn gọn và dễ quản lý hơn, đồng thời giúp giảm cái việc bị merge conflict. E nói vạy thoi chứ kiểu quờn gì cx conflict, nhưng sẽ giúp giảm cái conflict hơn là việc ai cũng chui vô `Program.cs` để config. *Cái này là e nói sơ qua thôi nha, mọi người nên tìm hiểu thêm về extensions*

### `UserManagementDemo.Application`

```text
UserManagementDemo.Application/  
│ ├── DTOs/  
│ ├── Interfaces/  
│ ├── Services/  
│ └── Exceptions/  
```

Project này là chứa mấy cái logic nghiệp vụ của cái web mình. Project API sẽ gọi trực tiếp các services trong đây. 
DTO và interface, custom exception cũng để trong đây

### `UserManagementDemo.Infrastructure`

```txt
UserManagementDemo.Infrastructure/  
├── Data/  
├── Clients/  
└── Services/  
```

Project này chứa các phần liên quan đến công nghệ và hệ thống bên ngoài, ví dụ như database, gửi email, gọi GitHub API, lưu file (Azure blob storage), gọi Python service (market pulse), hoặc kết nối với các service khác.

`Application` sẽ định nghĩa hệ thống cần làm gì thông qua interface, còn `Infrastructure` sẽ implement các interface đó bằng công nghệ cụ thể.

Các folder chính:
- `Data/`: chứa DbContext
- `Clients/`: chứa các class gọi API bên ngoài như GitHub, Gemini, ...
- `Services/`: chứa implementation thật của các service kỹ thuật như gửi email, lưu file, OTP.

## Frontend
E k đủ kiến thức để nói về cấu trúc project phía này :)))))
