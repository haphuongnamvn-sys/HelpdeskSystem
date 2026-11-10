using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using HelpdeskSystem.Helpers;
using HelpdeskSystem.Models;
using HelpdeskSystem.Models.ViewModels;

namespace HelpdeskSystem.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;
        private ApplicationDbContext _context;

        public AccountController()
        {
            _context = new ApplicationDbContext();
        }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
            _context = new ApplicationDbContext();
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Support login by Username or Email
            string userName = model.Email?.Trim();
            var userByEmail = _context.Users.FirstOrDefault(u => u.Email == userName || u.UserName == userName);
            if (userByEmail != null)
            {
                if (!userByEmail.IsActive)
                {
                    ModelState.AddModelError("", "Tài khoản này đã bị vô hiệu hóa. Vui lòng liên hệ Quản trị viên.");
                    return View(model);
                }
                userName = userByEmail.UserName;
            }

            var result = await SignInManager.PasswordSignInAsync(userName, model.Password, model.RememberMe, shouldLockout: false);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Tài khoản hoặc mật khẩu không đúng.");
                    return View(model);
            }
        }

        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber
                };
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await UserManager.AddToRoleAsync(user.Id, "EndUser");
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                    return RedirectToAction("Index", "Home");
                }
                AddErrors(result);
            }

            return View(model);
        }

        // GET / POST: /Account/LogOff
        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            Session?.Clear();
            Session?.Abandon();

            if (Request.Cookies[".AspNet.ApplicationCookie"] != null)
            {
                Response.Cookies.Add(new HttpCookie(".AspNet.ApplicationCookie") { Expires = DateTime.Now.AddDays(-1) });
            }
            if (Request.Cookies["__RequestVerificationToken"] != null)
            {
                Response.Cookies.Add(new HttpCookie("__RequestVerificationToken") { Expires = DateTime.Now.AddDays(-1) });
            }

            return RedirectToAction("Login", "Account");
        }

        // GET: /Account/Profile (UC02 - Quản lý tài khoản cá nhân)
        public ActionResult Profile()
        {
            var userId = User.Identity.GetUserId();
            var user = _context.Users.Find(userId);
            if (user == null)
            {
                return HttpNotFound();
            }

            var roles = UserManager.GetRoles(userId);
            var department = _context.Departments.Find(user.DepartmentId);

            var model = new ProfileViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                Position = user.Position,
                DepartmentName = department?.Name ?? "N/A",
                RoleName = GetRoleVietnameseName(roles.FirstOrDefault()),
                AvatarUrl = user.AvatarUrl
            };

            return View(model);
        }

        // POST: /Account/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Profile(ProfileViewModel model)
        {
            var userId = User.Identity.GetUserId();
            var user = await UserManager.FindByIdAsync(userId);
            if (user == null) return HttpNotFound();

            if (ModelState.IsValid)
            {
                user.FullName = model.FullName;
                user.PhoneNumber = model.PhoneNumber;
                user.Address = model.Address;
                user.Position = model.Position;
                if (!string.IsNullOrWhiteSpace(model.Email))
                {
                    user.Email = model.Email;
                }

                if (model.AvatarFile != null && model.AvatarFile.ContentLength > 0)
                {
                    var ext = Path.GetExtension(model.AvatarFile.FileName)?.ToLower();
                    if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".gif")
                    {
                        var (_, filePath, _) = FileHelper.SaveFile(model.AvatarFile, "avatars");
                        user.AvatarUrl = filePath;
                    }
                }

                var result = await UserManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Cập nhật thông tin cá nhân thành công!";
                    return RedirectToAction("Profile");
                }
                AddErrors(result);
            }

            var roles = UserManager.GetRoles(userId);
            var department = _context.Departments.Find(user.DepartmentId);
            model.DepartmentName = department?.Name ?? "N/A";
            model.RoleName = GetRoleVietnameseName(roles.FirstOrDefault());
            model.AvatarUrl = user.AvatarUrl;

            return View(model);
        }

        // GET: /Account/ChangePassword
        public ActionResult ChangePassword()
        {
            return View();
        }

        // POST: /Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                return RedirectToAction("Profile", "Account");
            }
            AddErrors(result);
            return View(model);
        }

        private string GetRoleVietnameseName(string role)
        {
            switch (role)
            {
                case "Admin": return "Quản trị viên";
                case "Manager": return "Quản lý";
                case "Technician": return "Nhân viên phụ trách";
                case "EndUser": return "Người dùng";
                default: return role ?? "N/A";
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }
                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
                if (_context != null)
                {
                    _context.Dispose();
                    _context = null;
                }
            }
            base.Dispose(disposing);
        }

        #region Helpers
        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }
        #endregion
    }
}
