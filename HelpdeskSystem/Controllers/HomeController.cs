using System;
using System.Web.Mvc;
using HelpdeskSystem.Models;
using HelpdeskSystem.Repositories;
using HelpdeskSystem.Services;

namespace HelpdeskSystem.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITicketService _ticketService;

        public HomeController()
        {
            var context = new ApplicationDbContext();
            _unitOfWork = new UnitOfWork(context);
            _ticketService = new TicketService(_unitOfWork);
        }

        // Chuyển hướng người dùng về trang chức năng chính tương ứng theo Biểu đồ Use Case:
        // - Quản trị viên: Quản lý tài khoản (/Admin/Users)
        // - Quản lý / Nhân viên phụ trách / Người dùng: Quản lý phiếu (/Ticket)
        public ActionResult Index()
        {
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Users", "Admin");
            }

            return RedirectToAction("Index", "Ticket");
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
