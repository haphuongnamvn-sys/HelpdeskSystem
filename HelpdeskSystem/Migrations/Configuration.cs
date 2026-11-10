using System;
using System.Data.Entity.Migrations;
using System.Linq;
using HelpdeskSystem.Models;
using HelpdeskSystem.Models.Domain;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace HelpdeskSystem.Migrations
{
    public sealed class Configuration : DbMigrationsConfiguration<ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true;
            ContextKey = "HelpdeskSystem.Models.ApplicationDbContext";
        }

        protected override void Seed(ApplicationDbContext context)
        {
            // === Roles ===
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
            string[] roleNames = { "Admin", "Manager", "Technician", "EndUser" };
            foreach (var roleName in roleNames)
            {
                if (!roleManager.RoleExists(roleName))
                {
                    roleManager.Create(new IdentityRole(roleName));
                }
            }

            // === Departments ===
            context.Departments.AddOrUpdate(d => d.DepartmentId,
                new Department { DepartmentId = 1, Name = "Phòng Hỗ trợ Kỹ thuật", Description = "IT Support", IsActive = true },
                new Department { DepartmentId = 2, Name = "Phòng Hỗ trợ Phần mềm", Description = "Software Support", IsActive = true },
                new Department { DepartmentId = 3, Name = "Phòng Quản trị Hệ thống", Description = "System Administration", IsActive = true }
            );
            context.SaveChanges();

            // === Categories ===
            context.Categories.AddOrUpdate(c => c.CategoryId,
                new Category { CategoryId = 1, Name = "Phần cứng", Description = "Hỗ trợ phần cứng", DepartmentId = 1, IsActive = true },
                new Category { CategoryId = 2, Name = "Mạng & Internet", Description = "Hỗ trợ mạng", DepartmentId = 1, IsActive = true },
                new Category { CategoryId = 3, Name = "Phần mềm văn phòng", Description = "Phần mềm VP", DepartmentId = 2, IsActive = true },
                new Category { CategoryId = 4, Name = "Phần mềm nội bộ", Description = "Phần mềm nội bộ", DepartmentId = 2, IsActive = true },
                new Category { CategoryId = 5, Name = "Tài khoản & Truy cập", Description = "Tài khoản", DepartmentId = 3, IsActive = true },
                new Category { CategoryId = 6, Name = "Email & Liên lạc", Description = "Email", DepartmentId = 3, IsActive = true },
                new Category { CategoryId = 7, Name = "Khác", Description = "Các yêu cầu khác", DepartmentId = null, IsActive = true }
            );

            // === Priorities ===
            context.Priorities.AddOrUpdate(p => p.PriorityId,
                new Priority { PriorityId = 1, Name = "Khẩn cấp", Level = 1, ColorCode = "#dc3545" },
                new Priority { PriorityId = 2, Name = "Cao", Level = 2, ColorCode = "#fd7e14" },
                new Priority { PriorityId = 3, Name = "Trung bình", Level = 3, ColorCode = "#ffc107" },
                new Priority { PriorityId = 4, Name = "Thấp", Level = 4, ColorCode = "#28a745" }
            );

            // === SLA Configs ===
            context.SLAConfigs.AddOrUpdate(s => s.SLAConfigId,
                new SLAConfig { SLAConfigId = 1, PriorityId = 1, ResponseTimeHours = 1, ResolutionTimeHours = 4, WarningThresholdPercent = 80 },
                new SLAConfig { SLAConfigId = 2, PriorityId = 2, ResponseTimeHours = 4, ResolutionTimeHours = 8, WarningThresholdPercent = 80 },
                new SLAConfig { SLAConfigId = 3, PriorityId = 3, ResponseTimeHours = 8, ResolutionTimeHours = 24, WarningThresholdPercent = 80 },
                new SLAConfig { SLAConfigId = 4, PriorityId = 4, ResponseTimeHours = 24, ResolutionTimeHours = 72, WarningThresholdPercent = 80 }
            );

            context.SaveChanges();

            // === Users ===
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));
            userManager.UserValidator = new UserValidator<ApplicationUser>(userManager)
            {
                AllowOnlyAlphanumericUserNames = false,
                RequireUniqueEmail = true
            };

            var users = new[]
            {
                new { Email = "admin@helpdesk.com", Password = "Admin@123", FullName = "Nguyễn Văn Admin", Role = "Admin", DeptId = (int?)null, Position = "Quản trị viên hệ thống" },
                new { Email = "manager@helpdesk.com", Password = "Manager@123", FullName = "Lê Quản Lý", Role = "Manager", DeptId = (int?)1, Position = "Trưởng bộ phận Hỗ trợ" },
                new { Email = "tech01@helpdesk.com", Password = "Tech@123", FullName = "Trần Văn Kỹ Thuật", Role = "Technician", DeptId = (int?)1, Position = "Nhân viên kỹ thuật phần cứng & mạng" },
                new { Email = "tech02@helpdesk.com", Password = "Tech@123", FullName = "Lê Thị Phần Mềm", Role = "Technician", DeptId = (int?)2, Position = "Nhân viên hỗ trợ phần mềm" },
                new { Email = "user01@helpdesk.com", Password = "User@123", FullName = "Phạm Văn Người Dùng", Role = "EndUser", DeptId = (int?)null, Position = "Chuyên viên Kế toán" },
                new { Email = "user02@helpdesk.com", Password = "User@123", FullName = "Hoàng Thị Khách Hàng", Role = "EndUser", DeptId = (int?)null, Position = "Chuyên viên Nhân sự" }
            };

            foreach (var u in users)
            {
                if (userManager.FindByEmail(u.Email) == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = u.Email,
                        Email = u.Email,
                        FullName = u.FullName,
                        DepartmentId = u.DeptId,
                        Position = u.Position,
                        Address = "Hà Nội",
                        IsActive = true,
                        CreatedDate = DateTime.Now,
                        EmailConfirmed = true
                    };
                    var result = userManager.Create(user, u.Password);
                    if (result.Succeeded)
                    {
                        userManager.AddToRole(user.Id, u.Role);
                    }
                }
            }

            context.SaveChanges();

            // === Knowledge Base Sample Articles ===
            var adminUser = userManager.FindByEmail("admin@helpdesk.com");
            string adminId = adminUser != null ? adminUser.Id : null;

            if (!context.KnowledgeBaseArticles.Any())
            {
                context.KnowledgeBaseArticles.AddOrUpdate(k => k.Title,
                    new KnowledgeBase
                    {
                        Title = "Hướng dẫn kết nối máy in văn phòng qua mạng LAN hoặc Wi-Fi",
                        CategoryId = 1, // Phần cứng
                        CreatedById = adminId,
                        ViewCount = 42,
                        CreatedDate = DateTime.Now.AddDays(-10),
                        Content = @"<p>Khi gặp vấn đề không tìm thấy máy in văn phòng trên máy tính của bạn, hãy thực hiện theo các bước sau:</p>
<ol>
    <li><strong>Kiểm tra nguồn và dây mạng:</strong> Đảm bảo máy in đã bật nguồn, đèn tín hiệu mạng (cổng LAN) sáng xanh hoặc màn hình máy in hiển thị biểu tượng Wi-Fi kết nối tốt.</li>
    <li><strong>Kiểm tra địa chỉ IP máy in:</strong> In trang cấu hình mạng (Network Configuration Report) từ màn hình menu của máy in để lấy địa chỉ IP (ví dụ: <code>192.168.1.200</code>).</li>
    <li><strong>Thêm máy in trên Windows:</strong>
        <ul>
            <li>Vào <em>Settings</em> &rarr; <em>Devices</em> &rarr; <em>Printers & scanners</em>.</li>
            <li>Bấm <strong>Add a printer or scanner</strong>.</li>
            <li>Nếu Windows không tự tìm thấy, chọn <em>The printer that I want isn't listed</em> &rarr; Chọn <em>Add a printer using an IP address or hostname</em>.</li>
            <li>Nhập địa chỉ IP máy in và bấm <em>Next</em> để hoàn tất cài đặt driver.</li>
        </ul>
    </li>
</ol>
<p>Nếu vẫn không in được, vui lòng khởi động lại dịch vụ Print Spooler (vào Services &rarr; tìm Print Spooler &rarr; Restart) hoặc gửi phiếu yêu cầu hỗ trợ.</p>"
                    },
                    new KnowledgeBase
                    {
                        Title = "Cách xử lý sự cố mạng Internet bị chập chờn hoặc không truy cập được",
                        CategoryId = 2, // Mạng & Internet
                        CreatedById = adminId,
                        ViewCount = 68,
                        CreatedDate = DateTime.Now.AddDays(-7),
                        Content = @"<p>Mạng Internet bị gián đoạn gây ảnh hưởng đến công việc. Dưới đây là các bước tự kiểm tra nhanh:</p>
<ol>
    <li><strong>Kiểm tra kết nối dây cáp hoặc Wi-Fi:</strong>
        <ul>
            <li>Nếu dùng dây LAN: Rút đầu cáp ra và cắm lại thật chặt, kiểm tra xem đèn cổng mạng ở máy tính có nháy sáng không.</li>
            <li>Nếu dùng Wi-Fi: Tắt kết nối Wi-Fi trên máy tính trong 10 giây và bật lại, hoặc kết nối thử vào mạng 5GHz của công ty.</li>
        </ul>
    </li>
    <li><strong>Làm mới địa chỉ IP bằng Command Prompt:</strong>
        <p>Mở cửa sổ <em>cmd</em> (Command Prompt) và chạy 2 lệnh sau:</p>
        <pre class='bg-light p-2 border'>ipconfig /release
ipconfig /renew
ipconfig /flushdns</pre>
    </li>
    <li><strong>Khởi động lại Router / Switch bàn làm việc:</strong> Nếu bàn của bạn có bộ chia mạng mini (Switch), hãy rút nguồn điện khoảng 15 giây và cắm lại.</li>
</ol>
<p>Nếu toàn bộ phòng ban đều mất kết nối, đây là sự cố diện rộng, vui lòng liên hệ trực tiếp hotline IT hoặc tạo phiếu ưu tiên Cao.</p>"
                    },
                    new KnowledgeBase
                    {
                        Title = "Quy trình mở khóa tài khoản nội bộ và đổi mật khẩu an toàn",
                        CategoryId = 5, // Tài khoản & Truy cập
                        CreatedById = adminId,
                        ViewCount = 95,
                        CreatedDate = DateTime.Now.AddDays(-5),
                        Content = @"<p>Để đảm bảo an toàn thông tin, hệ thống tự động khóa tạm thời tài khoản nếu nhập sai mật khẩu quá 5 lần liên tiếp.</p>
<h5>1. Khóa tạm thời 5 phút:</h5>
<p>Nếu bị khóa do gõ nhầm, hệ thống sẽ tự động mở lại sau <strong>5 phút</strong>. Vui lòng chờ hết thời gian này và kiểm tra kỹ phím <em>Caps Lock</em> hoặc bộ gõ tiếng Việt trước khi thử lại.</p>
<h5>2. Quên mật khẩu hoàn toàn:</h5>
<p>Nếu bạn không thể nhớ mật khẩu của mình:</p>
<ul>
    <li>Liên hệ trực tiếp quản trị viên hệ thống qua email: <code>admin@helpdesk.com</code>.</li>
    <li>Gửi kèm mã nhân viên và họ tên để IT cấp lại mật khẩu mặc định tạm thời.</li>
    <li>Sau khi đăng nhập được, bạn <strong>bắt buộc</strong> phải vào mục <em>Hồ sơ &rarr; Đổi mật khẩu</em> để thiết lập mật khẩu cá nhân mới.</li>
</ul>"
                    },
                    new KnowledgeBase
                    {
                        Title = "Hướng dẫn cài đặt và cấu hình phần mềm họp trực tuyến Microsoft Teams / Zoom",
                        CategoryId = 3, // Phần mềm VP
                        CreatedById = adminId,
                        ViewCount = 34,
                        CreatedDate = DateTime.Now.AddDays(-3),
                        Content = @"<p>Hướng dẫn chuẩn bị thiết bị micro, webcam và phần mềm trước các cuộc họp quan trọng:</p>
<ol>
    <li><strong>Cấp quyền truy cập Microphone và Camera trên Windows:</strong>
        <p>Vào <em>Settings &rarr; Privacy & security &rarr; Microphone / Camera</em>. Đảm bảo công tắc <em>Let apps access your microphone/camera</em> đang ở trạng thái <strong>On</strong>.</p>
    </li>
    <li><strong>Kiểm tra thiết bị âm thanh trên Teams / Zoom:</strong>
        <p>Trong ứng dụng, vào mục <em>Settings &rarr; Devices</em>:</p>
        <ul>
            <li>Mục Audio: Chọn đúng tai nghe (Headset) bạn đang sử dụng.</li>
            <li>Bấm <strong>Make a test call</strong> (trên Teams) hoặc <strong>Test Mic/Speaker</strong> (trên Zoom) để nghe lại giọng nói của mình.</li>
        </ul>
    </li>
</ol>"
                    }
                );
                context.SaveChanges();
            }
        }
    }
}
