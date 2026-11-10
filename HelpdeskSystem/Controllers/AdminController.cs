using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using HelpdeskSystem.Filters;
using HelpdeskSystem.Models;
using HelpdeskSystem.Models.Domain;
using HelpdeskSystem.Models.ViewModels;
using HelpdeskSystem.Repositories;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;

namespace HelpdeskSystem.Controllers
{
    [AuthorizeRoles("Admin")]
    public class AdminController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private ApplicationDbContext _context;

        public AdminController()
        {
            _context = new ApplicationDbContext();
            _unitOfWork = new UnitOfWork(_context);
        }

        private ApplicationUserManager UserManager
        {
            get { return HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>(); }
        }

        private ApplicationRoleManager RoleManager
        {
            get { return HttpContext.GetOwinContext().Get<ApplicationRoleManager>(); }
        }

        // ============================
        // USER MANAGEMENT (UC11 - 2.5.7)
        // ============================

        public ActionResult Users()
        {
            var users = _context.Users
                .Include(u => u.Department)
                .OrderBy(u => u.FullName)
                .ToList();

            var userVMs = new List<UserManagementVM>();
            foreach (var user in users)
            {
                var roles = UserManager.GetRoles(user.Id);
                userVMs.Add(new UserManagementVM
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Position = user.Position,
                    DepartmentName = user.Department?.Name ?? "N/A",
                    RoleName = roles.FirstOrDefault() ?? "N/A",
                    IsActive = user.IsActive,
                    CreatedDate = user.CreatedDate.ToString("dd/MM/yyyy HH:mm")
                });
            }

            return View(userVMs);
        }

        // GET: /Admin/UserCreate
        public ActionResult UserCreate()
        {
            var vm = new UserCreateVM
            {
                SelectedRole = "EndUser",
                Roles = RoleManager.Roles.OrderBy(r => r.Name).Select(r => new SelectListItem
                {
                    Value = r.Name,
                    Text = r.Name
                }).ToList(),
                Departments = _unitOfWork.Departments.GetAll()
                    .Where(d => d.IsActive)
                    .Select(d => new SelectListItem
                    {
                        Value = d.DepartmentId.ToString(),
                        Text = d.Name
                    }).ToList()
            };
            return View(vm);
        }

        // POST: /Admin/UserCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UserCreate(UserCreateVM model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email.Trim(),
                    Email = model.Email.Trim(),
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    Position = model.Position,
                    Address = model.Address,
                    DepartmentId = model.DepartmentId,
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    EmailConfirmed = true
                };

                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(model.SelectedRole))
                    {
                        await UserManager.AddToRoleAsync(user.Id, model.SelectedRole);
                    }
                    TempData["Success"] = $"Đã tạo tài khoản '{model.FullName}' thành công!";
                    return RedirectToAction("Users");
                }

                TempData["Error"] = "Không thể tạo tài khoản: " + string.Join(", ", result.Errors);
            }

            model.Roles = RoleManager.Roles.OrderBy(r => r.Name).Select(r => new SelectListItem
            {
                Value = r.Name,
                Text = r.Name,
                Selected = r.Name == model.SelectedRole
            }).ToList();
            model.Departments = _unitOfWork.Departments.GetAll()
                .Where(d => d.IsActive)
                .Select(d => new SelectListItem
                {
                    Value = d.DepartmentId.ToString(),
                    Text = d.Name,
                    Selected = d.DepartmentId == model.DepartmentId
                }).ToList();

            return View(model);
        }

        public ActionResult UserEdit(string id)
        {
            if (string.IsNullOrEmpty(id)) return new HttpStatusCodeResult(400);

            var user = _context.Users
                .Include(u => u.Department)
                .FirstOrDefault(u => u.Id == id);
            if (user == null) return HttpNotFound();

            var roles = UserManager.GetRoles(user.Id);
            var allRoles = RoleManager.Roles.Select(r => new SelectListItem
            {
                Value = r.Name,
                Text = r.Name,
                Selected = roles.Contains(r.Name)
            }).ToList();

            var departments = _unitOfWork.Departments.GetAll()
                .Where(d => d.IsActive)
                .Select(d => new SelectListItem
                {
                    Value = d.DepartmentId.ToString(),
                    Text = d.Name,
                    Selected = d.DepartmentId == user.DepartmentId
                }).ToList();

            ViewBag.Roles = allRoles;
            ViewBag.Departments = departments;
            ViewBag.CurrentRole = roles.FirstOrDefault();

            var vm = new UserManagementVM
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Position = user.Position,
                Address = user.Address,
                DepartmentId = user.DepartmentId,
                DepartmentName = user.Department?.Name,
                RoleName = roles.FirstOrDefault(),
                SelectedRole = roles.FirstOrDefault(),
                IsActive = user.IsActive,
                CreatedDate = user.CreatedDate.ToString("dd/MM/yyyy HH:mm"),
                Roles = allRoles,
                Departments = departments
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UserEdit(UserManagementVM model, string selectedRole)
        {
            var user = await UserManager.FindByIdAsync(model.Id);
            if (user == null) return HttpNotFound();

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            user.Position = model.Position;
            user.Address = model.Address;
            user.DepartmentId = model.DepartmentId;
            user.IsActive = model.IsActive;

            var result = await UserManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                var currentRoles = await UserManager.GetRolesAsync(user.Id);
                if (!string.IsNullOrEmpty(selectedRole) && !currentRoles.Contains(selectedRole))
                {
                    await UserManager.RemoveFromRolesAsync(user.Id, currentRoles.ToArray());
                    await UserManager.AddToRoleAsync(user.Id, selectedRole);
                }

                TempData["Success"] = "Cập nhật thông tin tài khoản và phân quyền thành công!";
                return RedirectToAction("Users");
            }

            TempData["Error"] = "Có lỗi xảy ra: " + string.Join(", ", result.Errors);
            return RedirectToAction("UserEdit", new { id = model.Id });
        }

        // POST: /Admin/UserResetPassword (UC11 - Cấp lại mật khẩu)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UserResetPassword(string id, string newPassword)
        {
            var user = await UserManager.FindByIdAsync(id);
            if (user == null) return HttpNotFound();

            string pwd = string.IsNullOrWhiteSpace(newPassword) ? "User@123" : newPassword.Trim();
            await UserManager.RemovePasswordAsync(user.Id);
            var result = await UserManager.AddPasswordAsync(user.Id, pwd);
            if (result.Succeeded)
            {
                TempData["Success"] = $"Đã cấp lại mật khẩu mới cho tài khoản '{user.FullName}' (Mật khẩu: {pwd})!";
            }
            else
            {
                TempData["Error"] = "Không thể cấp lại mật khẩu: " + string.Join(", ", result.Errors);
            }

            return RedirectToAction("UserEdit", new { id = id });
        }

        // POST: /Admin/UserDelete (UC11 - Xóa tài khoản)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UserDelete(string id)
        {
            if (id == User.Identity.GetUserId())
            {
                TempData["Error"] = "Không thể tự xóa tài khoản quản trị đang đăng nhập.";
                return RedirectToAction("Users");
            }

            var user = await UserManager.FindByIdAsync(id);
            if (user != null)
            {
                bool hasTickets = _context.Tickets.Any(t => t.CreatedById == id || t.AssignedToId == id);
                if (hasTickets)
                {
                    user.IsActive = false;
                    await UserManager.UpdateAsync(user);
                    TempData["Success"] = $"Tài khoản '{user.FullName}' có dữ liệu phiếu liên quan nên đã được chuyển sang trạng thái Ngừng hoạt động (Xóa mềm).";
                }
                else
                {
                    await UserManager.DeleteAsync(user);
                    TempData["Success"] = $"Đã xóa tài khoản '{user.FullName}' thành công.";
                }
            }

            return RedirectToAction("Users");
        }

        [HttpPost]
        public async Task<JsonResult> UserToggleActive(string id)
        {
            if (id == User.Identity.GetUserId())
            {
                return Json(new { success = false, message = "Không thể khóa tài khoản đang đăng nhập." });
            }
            var user = await UserManager.FindByIdAsync(id);
            if (user == null) return Json(new { success = false });

            user.IsActive = !user.IsActive;
            await UserManager.UpdateAsync(user);
            return Json(new { success = true, isActive = user.IsActive });
        }

        // ============================
        // DEPARTMENT MANAGEMENT (UC12 - 2.5.9)
        // ============================

        public ActionResult Departments()
        {
            var departments = _unitOfWork.Departments.GetAll()
                .OrderBy(d => d.Name)
                .ToList();
            return View(departments);
        }

        public ActionResult DepartmentCreate()
        {
            return View(new Department { IsActive = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DepartmentCreate(Department model)
        {
            if (ModelState.IsValid)
            {
                model.IsActive = true;
                _unitOfWork.Departments.Add(model);
                _unitOfWork.SaveChanges();
                TempData["Success"] = "Thêm phòng ban thành công!";
                return RedirectToAction("Departments");
            }
            return View(model);
        }

        public ActionResult DepartmentEdit(int id)
        {
            var department = _unitOfWork.Departments.GetById(id);
            if (department == null) return HttpNotFound();
            return View(department);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DepartmentEdit(Department model)
        {
            if (ModelState.IsValid)
            {
                var existing = _unitOfWork.Departments.GetById(model.DepartmentId);
                if (existing == null) return HttpNotFound();

                existing.Name = model.Name;
                existing.Description = model.Description;
                existing.IsActive = model.IsActive;
                _unitOfWork.SaveChanges();

                TempData["Success"] = "Cập nhật phòng ban thành công!";
                return RedirectToAction("Departments");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DepartmentDelete(int id)
        {
            var dept = _unitOfWork.Departments.GetById(id);
            if (dept != null)
            {
                bool inUse = _context.Users.Any(u => u.DepartmentId == id) || _context.Categories.Any(c => c.DepartmentId == id);
                if (inUse)
                {
                    dept.IsActive = false;
                    _unitOfWork.SaveChanges();
                    TempData["Success"] = $"Phòng ban '{dept.Name}' đang có nhân sự/danh mục liên kết nên đã chuyển sang Ngừng hoạt động.";
                }
                else
                {
                    _unitOfWork.Departments.Delete(dept);
                    _unitOfWork.SaveChanges();
                    TempData["Success"] = $"Đã xóa phòng ban '{dept.Name}' thành công.";
                }
            }
            return RedirectToAction("Departments");
        }

        // ============================
        // CATEGORY MANAGEMENT (UC13 - 2.5.8)
        // ============================

        public ActionResult Categories()
        {
            var categories = _unitOfWork.Categories.GetAll()
                .Include(c => c.Department)
                .OrderBy(c => c.Name)
                .ToList();
            return View(categories);
        }

        public ActionResult CategoryCreate()
        {
            ViewBag.Departments = new SelectList(
                _unitOfWork.Departments.GetAll().Where(d => d.IsActive).ToList(),
                "DepartmentId", "Name");
            return View(new Category { IsActive = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CategoryCreate(Category model)
        {
            if (ModelState.IsValid)
            {
                model.IsActive = true;
                _unitOfWork.Categories.Add(model);
                _unitOfWork.SaveChanges();
                TempData["Success"] = "Thêm danh mục sự cố thành công!";
                return RedirectToAction("Categories");
            }
            ViewBag.Departments = new SelectList(
                _unitOfWork.Departments.GetAll().Where(d => d.IsActive).ToList(),
                "DepartmentId", "Name", model.DepartmentId);
            return View(model);
        }

        public ActionResult CategoryEdit(int id)
        {
            var category = _unitOfWork.Categories.GetById(id);
            if (category == null) return HttpNotFound();

            ViewBag.Departments = new SelectList(
                _unitOfWork.Departments.GetAll().Where(d => d.IsActive).ToList(),
                "DepartmentId", "Name", category.DepartmentId);
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CategoryEdit(Category model)
        {
            if (ModelState.IsValid)
            {
                var existing = _unitOfWork.Categories.GetById(model.CategoryId);
                if (existing == null) return HttpNotFound();

                existing.Name = model.Name;
                existing.Description = model.Description;
                existing.DepartmentId = model.DepartmentId;
                existing.IsActive = model.IsActive;
                _unitOfWork.SaveChanges();

                TempData["Success"] = "Cập nhật danh mục sự cố thành công!";
                return RedirectToAction("Categories");
            }
            ViewBag.Departments = new SelectList(
                _unitOfWork.Departments.GetAll().Where(d => d.IsActive).ToList(),
                "DepartmentId", "Name", model.DepartmentId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CategoryDelete(int id)
        {
            var category = _unitOfWork.Categories.GetById(id);
            if (category != null)
            {
                bool inUse = _context.Tickets.Any(t => t.CategoryId == id) || _context.KnowledgeBaseArticles.Any(a => a.CategoryId == id);
                if (inUse)
                {
                    category.IsActive = false;
                    _unitOfWork.SaveChanges();
                    TempData["Success"] = $"Danh mục '{category.Name}' đang được sử dụng trong phiếu/bài viết nên đã được chuyển sang Ngừng sử dụng.";
                }
                else
                {
                    _unitOfWork.Categories.Delete(category);
                    _unitOfWork.SaveChanges();
                    TempData["Success"] = $"Đã xóa danh mục '{category.Name}' thành công.";
                }
            }
            return RedirectToAction("Categories");
        }

        // ============================
        // SLA & PRIORITY CONFIGURATION (UC14 - 2.6.8)
        // ============================

        public ActionResult SLAConfig()
        {
            var configs = _unitOfWork.SLAConfigs.GetAll()
                .Include(s => s.Priority)
                .OrderBy(s => s.Priority.Level)
                .ToList();
            ViewBag.Priorities = new SelectList(
                _unitOfWork.Priorities.GetAll().OrderBy(p => p.Level).ToList(), "PriorityId", "Name");
            return View(configs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SLAConfigSave(int? SLAConfigId, int PriorityId,
            int ResponseTimeHours, int ResolutionTimeHours, int WarningThresholdPercent)
        {
            if (ResponseTimeHours <= 0 || ResolutionTimeHours <= 0)
            {
                TempData["Error"] = "Thời hạn phản hồi và thời hạn xử lý phải là số nguyên dương.";
                return RedirectToAction("SLAConfig");
            }

            if (ResponseTimeHours > ResolutionTimeHours)
            {
                TempData["Error"] = "Thời hạn phản hồi không được lớn hơn thời hạn xử lý.";
                return RedirectToAction("SLAConfig");
            }

            if (SLAConfigId.HasValue && SLAConfigId.Value > 0)
            {
                var existing = _unitOfWork.SLAConfigs.GetById(SLAConfigId.Value);
                if (existing != null)
                {
                    existing.ResponseTimeHours = ResponseTimeHours;
                    existing.ResolutionTimeHours = ResolutionTimeHours;
                    existing.WarningThresholdPercent = WarningThresholdPercent;
                    _unitOfWork.SaveChanges();
                    TempData["Success"] = "Cập nhật thời hạn xử lý thành công!";
                }
            }
            else
            {
                var config = new SLAConfig
                {
                    PriorityId = PriorityId,
                    ResponseTimeHours = ResponseTimeHours,
                    ResolutionTimeHours = ResolutionTimeHours,
                    WarningThresholdPercent = WarningThresholdPercent
                };
                _unitOfWork.SLAConfigs.Add(config);
                _unitOfWork.SaveChanges();
                TempData["Success"] = "Thêm cấu hình thời hạn xử lý thành công!";
            }

            return RedirectToAction("SLAConfig");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _unitOfWork.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
