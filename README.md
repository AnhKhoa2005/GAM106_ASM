# Tài Liệu Dự Án GAM106 - Game Backend và Admin Dashboard

Đây là tài liệu chi tiết về dự án backend cho game, được xây dựng trên nền tảng ASP.NET Core. Tài liệu này nhằm mục đích giải thích cấu trúc, chức năng, và cách vận hành của dự án để giúp bạn hiểu rõ code và trả lời các câu hỏi liên quan.

## 1. Tổng Quan Dự Án

Dự án này là một hệ thống backend hoàn chỉnh cho một game, bao gồm hai thành phần chính:

1.  **Game API:** Cung cấp các API (endpoints) cho game client (ví dụ: một game làm bằng Unity, Unreal) để tương tác với server. Các chức năng bao gồm quản lý người chơi, dữ liệu game, giao dịch, v.v.
2.  **Admin Dashboard:** Một trang web quản trị cho phép Admin quản lý toàn bộ hệ thống, từ người chơi, vật phẩm, đến xem các số liệu thống kê.

## 2. Công Nghệ Sử Dụng

*   **Framework:** ASP.NET Core 8
*   **Ngôn ngữ:** C#
*   **Database:** PostgreSQL
*   **ORM (Object-Relational Mapping):** Entity Framework Core 8 (EF Core)
*   **Authentication (Xác thực):**
    *   **JSON Web Tokens (JWT):** Dùng cho Game API.
    *   **Cookie-based Authentication:** Dùng cho Admin Dashboard.
*   **Deployment:** Cấu hình sẵn cho Docker và Fly.io.

## 3. Cấu Trúc Thư Mục Quan Trọng

*   `📁 Controllers`: Chứa các API controllers, là nơi tiếp nhận và xử lý các HTTP request từ bên ngoài.
    *   `AuthController.cs`: Xử lý tất cả logic về **Đăng ký**, **Đăng nhập**, và **Quên mật khẩu**.
    *   `GameDataController.cs`: Cung cấp các API chính cho **game client** (lấy thông tin vật phẩm, người chơi, thực hiện hành động trong game).
    *   `AdminController.cs`: Cung cấp các API quản trị **chỉ dành cho Admin** (thêm/sửa/xóa người chơi, vật phẩm, v.v).
*   `📁 Models`: Định nghĩa cấu trúc cơ sở dữ liệu. Mỗi file C# trong này tương ứng với một bảng trong database PostgreSQL.
    *   `AppDbContext.cs`: "Trái tim" của Entity Framework, quản lý các `DbSet` (các bảng) và cấu hình mối quan hệ giữa chúng.
    *   `Player.cs`, `ItemSalesSheet.cs`, `Transaction.cs`, ...: Các entities của database.
*   `📁 Pages`: Chứa các trang web cho **Admin Dashboard** sử dụng công nghệ Razor Pages.
    *   `AdminDashboard.cshtml` và `AdminDashboard.cshtml.cs`: Trang tổng quan chính mà Admin thấy sau khi đăng nhập.
    *   Các thư mục con (`Players`, `Items`,...): Chứa các trang CRUD (Create, Read, Update, Delete) cho từng loại dữ liệu.
*   `📁 Services`: Chứa các lớp dịch vụ thực hiện các logic nghiệp vụ cụ thể.
    *   `EmailService.cs`: Chịu trách nhiệm gửi email (dùng cho OTP).
    *   `OtpService.cs`: Quản lý việc tạo và xác thực mã OTP.
*   `📁 DTOs (Data Transfer Objects)`: Các lớp đơn giản dùng để truyền dữ liệu giữa client và server (ví dụ: `LoginRequest.cs` chỉ chứa email và password).
*   `Program.cs`: Tệp tin khởi động của ứng dụng, nơi cấu hình mọi thứ: kết nối database, đăng ký services, thiết lập authentication, và pipeline xử lý request.

## 4. Hướng Dẫn Cài Đặt và Chạy Dự Án

**Yêu cầu:**
*   .NET 8 SDK
*   PostgreSQL Server

**Các bước thực hiện:**

1.  **Clone a project:**
    ```bash
    git clone <your-repo-url>
    cd GAM106_ASM
    ```
2.  **Cấu hình Connection String:**
    Mở file `GAM106_ASM/appsettings.Development.json`. Tìm đến phần `ConnectionStrings` và thay đổi cho đúng với thông tin database PostgreSQL của bạn.
    ```json
    "ConnectionStrings": {
      "DefaultConnection": "Host=localhost;Database=your_db_name;Username=your_username;Password=your_password"
    }
    ```

3.  **Chạy ứng dụng:**
    Mở terminal trong thư mục `GAM106_ASM` và chạy lệnh:
    ```bash
    dotnet run
    ```
    Ứng dụng sẽ khởi động và tự động tạo các bảng trong database (nếu chúng chưa tồn tại) dựa trên các models.

## 5. Giải Thích Các Luồng Chức Năng Cốt Lõi

### 5.1. Luồng Xác Thực (Authentication)

Dự án sử dụng một cơ chế xác thực **hỗn hợp (hybrid)** rất hay:

*   **Đối với Game Client (API):**
    1.  Client gửi yêu cầu `POST /api/Auth/Login` với email và password.
    2.  `AuthController` kiểm tra thông tin.
    3.  Nếu đúng, nó tạo ra một chuỗi **JWT** và trả về cho client.
    4.  Client lưu lại JWT này và gửi nó trong `Authorization` header của mỗi yêu cầu tiếp theo tới `GameDataController`.
    5.  Hệ thống sẽ xác thực JWT này để biết người dùng là ai và có quyền gì.

*   **Đối với Admin (Trang Web):**
    1.  Admin truy cập trang đăng nhập (`/Index`).
    2.  Khi đăng nhập thành công, server tạo ra một **Cookie** và lưu trong trình duyệt của Admin.
    3.  Trong các lần truy cập tiếp theo tới các trang trong `Pages`, trình duyệt tự động gửi cookie này lên, và hệ thống dùng nó để xác thực Admin.

> **⚠️ Điểm Cần Lưu Ý (Câu hỏi phỏng vấn):** Mật khẩu người dùng đang được lưu dưới dạng **văn bản thô (plain text)** trong database. Đây là một lỗ hổng bảo mật nghiêm trọng. Trong thực tế, mật khẩu phải luôn được **băm (hashed)** trước khi lưu (sử dụng các thuật toán như BCrypt, Argon2). Bạn có thể đề cập đây là một điểm cần cải tiến trong tương lai.

### 5.2. Luồng Quản Trị (Admin)

1.  **Phân Quyền:** Tất cả các API trong `AdminController` và các trang trong `Pages` đều được đánh dấu `[Authorize(Roles = "Admin")]`. Điều này đảm bảo chỉ người dùng có `Role` là "Admin" trong bảng `Player` mới có thể truy cập.
2.  **CRUD:** `AdminController` cung cấp đầy đủ các API để Thêm, Xem, Sửa, Xóa (CRUD) mọi dữ liệu trong game.
3.  **Xóa An Toàn:** Khi Admin thực hiện lệnh xóa một đối tượng có dữ liệu liên quan (ví dụ: xóa người chơi đã có giao dịch), API sẽ không xóa ngay mà trả về lỗi `409 Conflict` để yêu cầu xác nhận. Muốn xóa, Admin phải gửi thêm tham số `force=true`.
4.  **Ghi Log (Auditing):** Mọi hành động thêm/sửa/xóa của Admin đều được ghi lại vào bảng `AuditLogs`. Trang Admin Dashboard sẽ hiển thị các log này.

### 5.3. Luồng Lấy Dữ Liệu Game (Game Client)

*   `GameDataController` là nơi cung cấp dữ liệu cho game. Toàn bộ controller này được bảo vệ, yêu cầu phải có JWT hợp lệ.
*   Nó chứa nhiều API để lấy dữ liệu, từ những truy vấn đơn giản (lấy tất cả `Resource`) đến những truy vấn phức tạp (lấy `TopSellingItems` - top vật phẩm bán chạy). Các truy vấn này sử dụng **LINQ** và **Entity Framework Core** để thao tác với database một cách hiệu quả.

## 6. Cơ Sở Dữ Liệu (Database Schema)

Database được thiết kế để theo dõi mọi khía cạnh của game:

*   `Player`: Bảng trung tâm chứa thông tin người dùng.
*   `Character`: Nhân vật trong game của người chơi.
*   `ItemType`, `ItemSalesSheet`, `Transaction`: Quản lý hệ thống vật phẩm và giao dịch.
*   `Monster`, `MonsterKill`: Quản lý quái vật và lịch sử tiêu diệt.
*   `Quest`, `PlayerQuest`: Quản lý nhiệm vụ và tiến trình của người chơi.
*   `PlayHistory`: Ghi lại lịch sử các phiên chơi.
*   `AuditLog`: Ghi lại hành động của Admin.

Tên các bảng và các cột trong database đều theo chuẩn `snake_case` (ví dụ: `player_id`, `item_sales_sheet`), được cấu hình trong `AppDbContext.cs`.

## 7. Triển Khai (Deployment)

Dự án đã được cấu hình sẵn để triển khai bằng hai cách hiện đại:
*   **Dockerfile:** Cho phép bạn "đóng gói" ứng dụng vào một container, có thể chạy trên bất kỳ môi trường nào hỗ trợ Docker.
*   **fly.toml:** Tệp cấu hình để triển khai ứng dụng lên nền tảng **Fly.io**. Code trong `Program.cs` cũng được viết để tương thích với Fly.io (ví dụ: cấu hình port, forwarded headers).

---
Chúc bạn may mắn với buổi bảo vệ! Hãy đọc kỹ tài liệu này, bạn sẽ có thể tự tin trả lời các câu hỏi về dự án.