# RentGo local

## Khởi động

Trong PowerShell, mở backend và chạy profile HTTPS:

```powershell
dotnet dev-certs https --trust
dotnet run --project 'D:/Công nghê phân mềm/Finnal project/CarRentalBackend/CarRental.API' --launch-profile https
```

Trong cửa sổ PowerShell khác, mở thư mục chứa README này rồi chạy:

```powershell
node --use-system-ca serve.mjs
```

Mở **http://127.0.0.1:5500/**. Dừng Live Server nếu cổng 5500 đang được dùng. Node 24 được dùng để kiểm thử. Có thể đặt `RENTGO_PORT` để chọn cổng website khác. Nếu backend chạy profile `http` riêng trên 5183, đặt `$env:RENTGO_BACKEND = 'http://localhost:5183'` trước khi chạy Node. Cách này chỉ dành cho phát triển local.

Node chỉ lắng nghe trên loopback. Trang web, CSS, JS, API và ảnh đều đi qua cùng địa chỉ trong trình duyệt. Node chuyển tiếp `/api/*` và `/uploads/vehicles/*` tới backend local. Ảnh vẫn do backend lưu ở `wwwroot/uploads/vehicles`. Với backend HTTPS, Node xác minh chứng chỉ qua kho tin cậy của Windows. Cấu hình này không cần CORS hoặc thay đổi `Program.cs`.

## Trang và API

| Trang | API và hành vi |
|---|---|
| Trang chủ và danh sách xe | `GET /api/vehicles`, `GET /api/vehicle-models`, `GET /api/vehicles/{id}/images`. Ghép xe với mẫu để lấy giá, nhóm xe và thông số. Tìm kiếm, lọc và phân trang trên frontend. |
| Chi tiết xe | `GET /api/vehicles/{id}`, `GET /api/vehicle-models/{id}`, `GET /api/vehicles/{id}/images`. |
| Đăng ký và đăng nhập | `POST /api/auth/register`, `POST /api/auth/login`. JWT lưu theo phiên tab, xóa khi đăng xuất, khi phát hiện hết hạn hoặc khi nhận 401 từ API có bảo vệ. Hồ sơ hiển thị những trường do API login trả về. |
| Giỏ và đặt thuê | Tải lại xe, gửi `POST /api/rental-orders` với ID xe và ngày giờ ISO có múi giờ. Hiển thị mã, giá và trạng thái do server trả. API tạo đơn kiểm tra trùng lịch và trả 409. |
| Đơn của tôi | `GET /api/rental-orders/my?page=…&pageSize=20`, `GET /api/rental-orders/{id}`, `PUT /api/rental-orders/{id}/cancel`. Bộ lọc trạng thái áp dụng trên trang đơn hiện tại. |
| Quản trị mẫu xe và xe | GET, POST, PUT, DELETE trên `/api/vehicle-models` và `/api/vehicles`. |
| Quản trị ảnh | GET và POST multipart trên `/api/vehicles/{id}/images`, PUT `…/images/primary`, DELETE `…/images/{imageId}`. |
| Quản trị đơn | Tra cứu theo ID với `GET /api/rental-orders/{id}`. Hủy bằng PUT `…/cancel`, giao xe và hoàn tất bằng PUT `…/status` theo trạng thái backend cho phép. |
| Thanh toán và người dùng | Thanh toán hiển thị “Thanh toán đang phát triển”. Trang người dùng hiển thị thông báo chưa có API quản trị người dùng. |

Backend hiện thiếu API danh sách đơn toàn hệ thống, sửa lịch đơn đã tạo, hồ sơ riêng, đăng xuất phía server và kiểm tra lịch trống riêng. Quản trị đơn sử dụng ID số. Việc bổ sung danh sách đơn toàn hệ thống được hoãn để tránh thay đổi phần Rental Orders trong khi phiên Payments đang làm việc.
