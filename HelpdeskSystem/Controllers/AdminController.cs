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
        // USER MANAGEMENT
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
                    DepartmentName = user.Department?.Name ?? "N/A",
                    RoleName = roles.FirstOrDefault() ?? "N/A",
                    IsActive = user.IsActive,
                    CreatedDate = user.CreatedDate.ToString("dd/MM/yyyy HH:mm")
                });
            }

            return View(userVMs);
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
            user.DepartmentId = model.DepartmentId;
            user.IsActive = model.IsActive;

            var result = await UserManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // Update role
                var currentRoles = await UserManager.GetRolesAsync(user.Id);
                if (!string.IsNullOrEmpty(selectedRole) && !currentRoles.Contains(selectedRole))
                {
                    await UserManager.RemoveFromRolesAsync(user.Id, currentRoles.ToArray());
                    await UserManager.AddToRoleAsync(user.Id, selectedRole);
                }

                TempData["Success"] = "Cập nhật người dùng thành công!";
                return RedirectToAction("Users");
            }

            TempData["Error"] = "Có lỗi xảy ra: " + string.Join(", ", result.Errors);
            return RedirectToAction("UserEdit", new { id = model.Id });
        }

        // ============================
        // DEPARTMENT MANAGEMENT
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
            return View(new Department());
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

        // ============================
        // CATEGORY MANAGEMENT
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
            return View(new Category());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CategoryCreate(Category model)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Categories.Add(model);
                _unitOfWork.SaveChanges();
                TempData["Success"] = "Thêm danh mục thành công!";
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
                _unitOfWork.SaveChanges();

                TempData["Success"] = "Cập nhật danh mục thành công!";
                return RedirectToAction("Categories");
            }
            ViewBag.Departments = new SelectList(
                _unitOfWork.Departments.GetAll().Where(d => d.IsActive).ToList(),
                "DepartmentId", "Name", model.DepartmentId);
            return View(model);
        }

        // ============================
        // SLA CONFIGURATION
        // ============================

        public ActionResult SLAConfig()
        {
            var configs = _unitOfWork.SLAConfigs.GetAll()
                .Include(s => s.Priority)
                .OrderBy(s => s.Priority.Level)
                .ToList();
            ViewBag.Priorities = new SelectList(
                _unitOfWork.Priorities.GetAll().ToList(), "PriorityId", "Name");
            return View(configs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SLAConfigSave(int? SLAConfigId, int PriorityId,
            int ResponseTimeHours, int ResolutionTimeHours, int WarningThresholdPercent)
        {
            if (SLAConfigId.HasValue && SLAConfigId.Value > 0)
            {
                // Update existing
                var existing = _unitOfWork.SLAConfigs.GetById(SLAConfigId.Value);
                if (existing != null)
                {
                    existing.ResponseTimeHours = ResponseTimeHours;
                    existing.ResolutionTimeHours = ResolutionTimeHours;
                    existing.WarningThresholdPercent = WarningThresholdPercent;
                    _unitOfWork.SaveChanges();
                    TempData["Success"] = "Cập nhật cấu hình SLA thành công!";
                }
            }
            else
            {
                // Create new
                var config = new SLAConfig
                {
                    PriorityId = PriorityId,
                    ResponseTimeHours = ResponseTimeHours,
                    ResolutionTimeHours = ResolutionTimeHours,
                    WarningThresholdPercent = WarningThresholdPercent
                };
                _unitOfWork.SLAConfigs.Add(config);
                _unitOfWork.SaveChanges();
                TempData["Success"] = "Thêm cấu hình SLA thành công!";
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
