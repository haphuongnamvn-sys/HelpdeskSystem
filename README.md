# Hệ Thống Tiếp Nhận & Quản Lý Yêu Cầu Hỗ Trợ Kỹ Thuật IT (IT Helpdesk System)

[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.8-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET MVC](https://img.shields.io/badge/ASP.NET%20MVC-5.2.9-blue?logo=microsoft)](https://dotnet.microsoft.com/apps/aspnet/mvc)
[![Entity Framework](https://img.shields.io/badge/Entity%20Framework-6.0-68217A)](https://docs.microsoft.com/ef/)
[![C#](https://img.shields.io/badge/Language-C%23-239120?logo=c-sharp&logoColor=white)](https://docs.microsoft.com/dotnet/csharp/)
[![Bootstrap](https://img.shields.io/badge/UI-Bootstrap%205-7952B3?logo=bootstrap&logoColor=white)](https://getbootstrap.com/)
[![SQL Server](https://img.shields.io/badge/Database-SQL%20Server-CC292B?logo=microsoft-sql-server&logoColor=white)](https://www.microsoft.com/sql-server/)

> **Hệ thống Helpdesk** là giải pháp phần mềm quản lý và điều phối các yêu cầu hỗ trợ kỹ thuật công nghệ thông tin (ITSM / IT Helpdesk) trong doanh nghiệp, được xây dựng theo chuẩn quy trình quản lý dịch vụ CNTT (ITIL). Hệ thống tối ưu hóa quy trình từ lúc phát sinh sự cố, tiếp nhận, phân công kỹ thuật viên, giám sát cam kết dịch vụ (SLA) đến khi giải quyết và đánh giá chất lượng.

---

## 📌 Mục Lục
- [1. Các Tính Năng Nổi Bật](#1-các-tính-năng-nổi-bật)
- [2. Quy Trình Vòng Đời Phiếu Hỗ Trợ (Ticket Lifecycle)](#2-quy-trình-vòng-đời-phiếu-hỗ-trợ-ticket-lifecycle)
- [3. Công Nghệ & Kiến Trúc Sử Dụng](#3-công-nghệ--kiến-trúc-sử-dụng)
- [4. Cấu Trúc Thư Mục Dự Án](#4-cấu-trúc-thư-mục-dự-án)
- [5. Hướng Dẫn Cài Đặt & Khởi Chạy](#5-hướng-dẫn-cài-đặt--khởi-chạy)
- [6. Tài Khoản Thử Nghiệm (Demo Accounts)](#6-tài-khoản-thử-nghiệm-demo-accounts)

---

## 1. Các Tính Năng Nổi Bật

### 👥 Phân Quyền Vai Trò (Role-Based Access Control - RBAC)
Hệ thống phân định quyền hạn chặt chẽ theo 4 nhóm người dùng:
- **Quản trị viên (Admin):** Quản lý tài khoản, phòng ban, danh mục sự cố, cấu hình SLA và toàn quyền hệ thống.
- **Trưởng bộ phận / Quản lý IT (Manager):** Giám sát tình trạng sự cố toàn công ty, phân công kỹ thuật viên chịu trách nhiệm cho từng phiếu, theo dõi tiến độ SLA và xuất báo cáo thống kê.
- **Kỹ thuật viên (Technician):** Tiếp nhận sự cố được phân công, cập nhật tiến độ xử lý (`In Progress`, `Pending`, `Resolved`), trao đổi với người dùng và nhập giải pháp khắc phục.
- **Người dùng / Nhân viên (EndUser):** Tạo phiếu yêu cầu hỗ trợ, đính kèm tài liệu/hình ảnh lỗi, theo dõi tiến độ xử lý theo thời gian thực, trao đổi tin nhắn và đánh giá chất lượng hỗ trợ (Rating 1 - 5 sao).

---

### 🎫 Quản Lý Phiếu Yêu Cầu (Ticket Management)
- **Tạo phiếu trực quan:** Hỗ trợ nhập tiêu đề, danh mục, mức độ ưu tiên và đính kèm nhiều tệp tài liệu/ảnh chụp màn hình sự cố (có tính năng xem trước và hủy tệp bằng nút xóa trực tiếp).
- **Chi tiết phiếu đa góc nhìn:** Thiết kế dạng khối thống nhất theo chuẩn giao diện doanh nghiệp (Enterprise UI), chia tab chuyên biệt:
  - *Thông tin phiếu & Mốc SLA:* Hiển thị chi tiết mô tả lỗi, tệp đính kèm, mốc thời gian tiếp nhận/giải quyết và cảnh báo vi phạm SLA.
  - *Phân công / Thao tác xử lý:* Giao diện phân công dành riêng cho Quản lý; nút chuyển trạng thái quy trình dành cho Kỹ thuật viên.
  - *Nội dung trao đổi (Ticket Chat):* Khung chat hai chiều dạng Messenger giữa người dùng và kỹ thuật viên phụ trách, hỗ trợ đính kèm tệp tin trao đổi.
  - *Lịch sử tiến độ (Audit Log):* Bảng theo dõi toàn bộ lịch sử thay đổi trạng thái, người thực hiện và mốc thời gian cố định tiêu đề khi cuộn.

---

### ⏱️ Cơ Chế Cam Kết Mức Độ Dịch Vụ (SLA Engine)
- **Tính toán theo giờ hành chính thực tế:** Thuật toán tự động tính toán hạn hoàn thành (`SLADueDate`) dựa trên khung giờ làm việc tiêu chuẩn (8h00 - 17h00) và tự động bỏ qua các ngày nghỉ cuối tuần (Thứ Bảy, Chủ Nhật).
- **Cảnh báo vi phạm:** Tự động đánh giá và gắn nhãn trực quan các phiếu đúng hạn hoặc đã quá hạn cam kết SLA.

---

### 🤖 Trợ Lý Ảo Thông Minh & Cơ Sở Tri Thức (Chatbot & Knowledge Base)
- **Floating Messenger Chatbot:** Nút chat nổi góc dưới màn hình giúp nhân viên tra cứu nhanh cách khắc phục các sự cố thường gặp (mạng máy tính, phần mềm, máy in, tài khoản).
- **Cơ sở tri thức (Knowledge Base):** Kho bài viết hướng dẫn xử lý sự cố kỹ thuật, cho phép tìm kiếm theo danh mục và đánh giá mức độ hữu ích của bài viết.

---

### 📊 Báo Cáo & Thống Kê Trực Quan (Analytics & Reports)
- Bảng điều khiển (Dashboard) với các chỉ số đo lường hiệu suất (KPI) trực quan.
- Biểu đồ thống kê động qua **Chart.js**:
  - Tỷ lệ phiếu theo trạng thái (`Open`, `In Progress`, `Resolved`, `Closed`).
  - Phân bổ sự cố theo mức độ ưu tiên và danh mục.
  - Tỷ lệ tuân thủ cam kết SLA.
- Hỗ trợ xuất dữ liệu báo cáo chi tiết ra file **Excel**.

---

## 2. Quy Trình Vòng Đời Phiếu Hỗ Trợ (Ticket Lifecycle)

```
[Khởi tạo phiếu] (Open)
        │
        ▼
[Phân công KTV] (Assigned) ─── Quản lý IT chỉ định KTV
        │
        ▼
[Đang xử lý] (InProgress) ─── KTV bắt đầu khắc phục sự cố
        │
   ┌────┴─────────────────────────┐
   ▼                              ▼
[Tạm dừng] (Pending)      [Đã giải quyết] (Resolved)
(Chờ phản hồi user)       (KTV nhập giải pháp)
   │                              │
   └──────────► [InProgress]      ├────────────────────────┐
                                  ▼                        ▼
                        [Nghiệm thu & Đánh giá]   [Mở lại phiếu]
                              (Closed)               (Reopened)
```

---

## 3. Công Nghệ & Kiến Trúc Sử Dụng

### Backend
- **Ngôn ngữ:** C# (.NET Framework 4.8)
- **Framework:** ASP.NET MVC 5
- **ORM / Truy xuất dữ liệu:** Entity Framework 6 (Code First)
- **Bảo mật & Xác thực:** ASP.NET Identity 2.0 (OWIN Authentication, Password Hashing, Anti-Forgery Tokens)
- **Mô hình thiết kế:** MVC 3 lớp, Repository Pattern & Unit of Work Pattern

### Frontend
- **View Engine:** Razor View (`.cshtml`)
- **Giao diện:** HTML5, CSS3, Bootstrap 5.2
- **Thư viện bổ trợ:** jQuery, FontAwesome 6, Chart.js

### Cơ Sở Dữ Liệu
- **Hệ quản trị CSDL:** Microsoft SQL Server / SQL Server Express LocalDB

---

## 4. Cấu Trúc Thư Mục Dự Án

```
HelpdeskSystem/
├── App_Start/              # Cấu hình Bundle, Filter, Identity, Route
├── Controllers/            # Bộ điều khiển (Account, Admin, Ticket, Chatbot, Report,...)
├── Helpers/                # Các lớp tiện ích (Mã phiếu tự sinh, xử lý File)
├── Migrations/             # Cấu hình EF Migrations & Dữ liệu mẫu (Seed Data)
├── Models/
│   ├── Domain/             # Thực thể CSDL (Ticket, Category, Department, SLAConfig,...)
│   └── ViewModels/         # Các lớp mô hình giao diện (DashboardVM, TicketDetailVM,...)
├── Repositories/           # Mẫu thiết kế Generic Repository & Unit of Work
├── Services/               # Nghiệp vụ xử lý (TicketService, NotificationService, SLAService)
├── Views/                  # Giao diện Razor Pages (.cshtml)
│   ├── Account/            # Đăng nhập, thông tin cá nhân, đổi mật khẩu
│   ├── Admin/              # Quản lý người dùng, phòng ban, danh mục, SLA
│   ├── Chatbot/            # Giao diện trợ lý ảo
│   ├── KnowledgeBase/      # Cơ sở tri thức & bài viết hướng dẫn
│   ├── Report/             # Thống kê & báo cáo
│   ├── Shared/             # Layout chung, Header, Sidebar, Chatbot Widget
│   └── Ticket/             # Tạo, xem chi tiết, phân công, danh sách phiếu
├── Web.config              # Cấu hình hệ thống & chuỗi kết nối CSDL (Connection String)
├── HelpdeskSystem.sln      # Solution Visual Studio
└── README.md               # Tài liệu dự án
```

---

## 5. Hướng Dẫn Cài Đặt & Khởi Chạy

### Yêu Cầu Môi Trường
- **Hệ điều hành:** Windows 10 / 11
- **Phần mềm:** Visual Studio 2019 / 2022 (cài đặt workload **ASP.NET and web development**)
- **.NET Framework:** Phiên bản 4.8
- **Cơ sở dữ liệu:** SQL Server LocalDB (mặc định tích hợp sẵn trong Visual Studio) hoặc SQL Server Express

### Các Bước Cài Đặt

1. **Clone repository về máy:**
   ```bash
   git clone https://github.com/haphuongnamvn-sys/HelpdeskSystem.git
   cd HelpdeskSystem
   ```

2. **Mở dự án:**
   Mở file giải pháp `HelpdeskSystem.sln` bằng Visual Studio.

3. **Cấu hình chuỗi kết nối CSDL (Tùy chọn):**
   Kiểm tra file `HelpdeskSystem/Web.config`, mặc định sử dụng SQL Server LocalDB:
   ```xml
   <connectionStrings>
     <add name="DefaultConnection" 
          connectionString="Data Source=(LocalDb)\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\HelpdeskSystem.mdf;Initial Catalog=HelpdeskSystem;Integrated Security=True" 
          providerName="System.Data.SqlClient" />
   </connectionStrings>
   ```

4. **Khôi phục gói thư viện (Restore NuGet Packages):**
   - Chuột phải vào Solution trong Visual Studio &rarr; chọn **Restore NuGet Packages**.
   - Hoặc chạy lệnh trong *Package Manager Console*:
     ```powershell
     Update-Package -reinstall
     ```

5. **Khởi tạo cơ sở dữ liệu & Dữ liệu mẫu:**
   Mở cửa sổ *Package Manager Console* trong Visual Studio và chạy:
   ```powershell
   Update-Database
   ```
   *(Hệ thống sẽ tự động tạo bảng và nạp sẵn dữ liệu phòng ban, danh mục, SLA, tài khoản mẫu).*

6. **Chạy ứng dụng:**
   Nhấn **F5** hoặc chọn **IIS Express (Google Chrome / Edge)** để bắt đầu trải nghiệm.

---

## 6. Tài Khoản Thử Nghiệm (Demo Accounts)

Hệ thống đã nạp sẵn các tài khoản demo tương ứng với 4 vai trò để kiểm thử:

| Vai Trò | Email Đăng Nhập | Mật Khẩu Mặc Định | Mô Tả Quyền Hạn |
| :--- | :--- | :--- | :--- |
| **Quản trị viên (Admin)** | `admin@helpdesk.com` | `Admin@123` | Toàn quyền cấu hình hệ thống, quản lý User, Phòng ban, SLA |
| **Quản lý IT (Manager)** | `manager@helpdesk.com` | `Manager@123` | Phân công kỹ thuật viên, theo dõi SLA, xuất báo cáo |
| **Kỹ thuật viên 1 (Technician)** | `tech01@helpdesk.com` | `Tech@123` | Xử lý sự cố phần cứng & mạng |
| **Kỹ thuật viên 2 (Technician)** | `tech02@helpdesk.com` | `Tech@123` | Xử lý sự cố phần mềm văn phòng |
| **Người dùng 1 (EndUser)** | `user01@helpdesk.com` | `User@123` | Gửi yêu cầu hỗ trợ (Phòng Kế toán) |
| **Người dùng 2 (EndUser)** | `user02@helpdesk.com` | `User@123` | Gửi yêu cầu hỗ trợ (Phòng Nhân sự) |

---

## 📄 Bản Quyền & Tác Giả
- **Tác giả:** Hà Phương Nam ([@haphuongnamvn-sys](https://github.com/haphuongnamvn-sys))
- **Đề tài:** Đồ án Chuyên ngành / Hệ thống Quản lý Yêu cầu Kỹ thuật Helpdesk (ITSM)
