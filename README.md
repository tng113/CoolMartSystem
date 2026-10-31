# Cool Mart – Hệ thống Quản trị & Bán lẻ (POS)

Đồ án môn **Lập trình ứng dụng** (CCQ2411C).
Hệ thống quản lý cửa hàng tiện lợi Cool Mart gồm **Backend ASP.NET Core Web API** và **Frontend WinForms**.

## 1. Công nghệ sử dụng
| Thành phần | Công nghệ |
|---|---|
| Backend | ASP.NET Core Web API (.NET 8), Entity Framework Core, ADO.NET, JWT Bearer, Swagger |
| Cơ sở dữ liệu | SQL Server (EF Core Code-First + Migrations + Seed Data) |
| Frontend | Windows Forms (.NET 8), HttpClient |
| IDE | Visual Studio 2022 |

## 2. Cấu trúc thư mục
```
CoolMartSystem/
├── CoolMartSystem.sln
├── CoolMart.API/            # Backend Web API
│   ├── Controllers/         # Auth, Categories, Products, Customers, Checkout, Orders, Reports, Users, AdoProduct
│   ├── Models/              # Category, Product, Customer, User, Order, OrderDetail
│   ├── Data/                # CoolMartDbContext (+ Seed Data)
│   ├── Migrations/
│   └── Program.cs           # JWT, CORS, Swagger, Stored Procedure
└── CoolMart.WinForms/       # Frontend WinForms (POS, quản lý, báo cáo)
```

## 3. Yêu cầu cài đặt
- Visual Studio 2022 (workload *ASP.NET and web development* và *.NET desktop development*)
- .NET 8 SDK
- SQL Server (LocalDB, Express hoặc bản đầy đủ)

## 4. Hướng dẫn chạy Backend (Web API)
1. Mở file `CoolMartSystem.sln` bằng Visual Studio.
2. Mở `CoolMart.API/appsettings.json`, sửa chuỗi kết nối nếu cần:
   ```json
   "DefaultConnection": "Server=localhost;Database=CoolMartDbContext;Trusted_Connection=True;TrustServerCertificate=True;"
   ```
3. Tạo cơ sở dữ liệu: vào **Tools → NuGet Package Manager → Package Manager Console**, chọn **Default project = CoolMart.API**, chạy:
   ```
   Update-Database
   ```
   Lệnh này tạo database và nạp sẵn dữ liệu mẫu (5 nhóm hàng, 65 sản phẩm, khách hàng, tài khoản).
4. Chuột phải project `CoolMart.API` → **Set as Startup Project**, chọn profile **https** rồi bấm **F5**.
5. Trình duyệt mở Swagger tại `https://localhost:7182/swagger`.
   Dùng `POST /api/Auth/login` lấy token, bấm **Authorize** và nhập `Bearer <token>` để gọi các API.

> Khi API khởi động, hệ thống tự tạo các Stored Procedure báo cáo `sp_RevenueByDay`, `sp_RevenueByShift`.

## 5. Hướng dẫn chạy Frontend (WinForms)
1. **Phải chạy Backend trước** (địa chỉ API đặt trong `CoolMart.WinForms/ApiClientService.cs`: `https://localhost:7182/api/`).
2. Chuột phải project `CoolMart.WinForms` → **Debug → Start New Instance**.
3. Đăng nhập bằng một trong các tài khoản bên dưới.

*Mẹo:* chuột phải Solution → **Configure Startup Projects** → chọn *Multiple startup projects* để khởi động cả hai cùng lúc (API đặt trên WinForms).

## 6. Tài khoản mẫu
| Tài khoản | Mật khẩu | Vai trò | Quyền |
|---|---|---|---|
| `admin01` | `123` | Admin | Toàn quyền |
| `cashier01` | `123` | Cashier | Chỉ màn hình Thu ngân (POS) |
| `ware01` | `123` | Warehouse | Quản lý kho, danh mục, sản phẩm |

## 7. Nội dung đã thực hiện
**Backend**
- Mô hình dữ liệu 6 bảng (Category, Product, Customer, User, Order, OrderDetail), Code-First, Migration và Seed Data.
- CRUD nhóm hàng, sản phẩm; tìm kiếm LINQ theo tên/barcode, lọc theo nhóm hàng, phân trang (Skip/Take), `Include`, `AsNoTracking`.
- Xác thực **JWT**, `[Authorize]`, phân quyền theo Role (Admin/Cashier/Warehouse), Swagger có nút Authorize.
- Tra cứu sản phẩm bằng barcode với **ADO.NET** (`SqlDataReader`, tham số hóa chống SQL Injection).
- Thanh toán bằng **SqlTransaction**: lưu hóa đơn, trừ tồn kho, cộng/trừ điểm thưởng; lỗi thì Rollback.
- Báo cáo: KPI, doanh thu theo ngày/ca (**Stored Procedure**), Top 5 sản phẩm bán chạy, tỉ trọng theo ngành hàng; xem lịch sử hóa đơn.
- DTO, validation (`[Required]`, `[StringLength]`, `[Range]`), CORS.

**Frontend WinForms**
- Giao diện Shell (sidebar, header), đăng nhập lưu token trong `SessionManager`, `ApiClientService` tự gắn Bearer Token.
- Ẩn/hiện menu theo vai trò.
- POS: quét barcode, gộp số lượng, tính tổng/VAT/voucher, đổi điểm thưởng, tính tiền thối, thanh toán.
- Quản lý sản phẩm (lưới dữ liệu, phân trang, cảnh báo tồn kho thấp), quản lý khách hàng, nhập kho, xuất CSV.
- Dashboard báo cáo: thẻ KPI, biểu đồ cột doanh thu theo ngày, Top 5 sản phẩm, lịch sử hóa đơn.

## 8. Tác giả
Sinh viên thực hiện: Nguyễn Thị Thu Nguyệt – Lớp CCQ2411C.
