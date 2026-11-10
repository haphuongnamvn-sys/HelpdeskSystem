using HelpdeskSystem.Filters;
using System;
using System.Linq;
using System.Web.Mvc;
using HelpdeskSystem.Models;
using HelpdeskSystem.Repositories;
using Microsoft.AspNet.Identity;

namespace HelpdeskSystem.Controllers
{
    [AuthorizeRoles("EndUser", "Manager", "Technician")]
    public class NotificationController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public NotificationController()
        {
            _unitOfWork = new UnitOfWork(new ApplicationDbContext());
        }

        // GET: /Notification/GetUnread
        [HttpGet]
        public JsonResult GetUnread()
        {
            var userId = User.Identity.GetUserId();
            var notifications = _unitOfWork.Notifications
                .Find(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedDate)
                .Take(10)
                .Select(n => new
                {
                    notificationId = n.NotificationId,
                    title = n.Title,
                    message = n.Message,
                    url = n.Url,
                    timeAgo = ""
                })
                .ToList();

            // Calculate time ago on the server side
            var now = DateTime.Now;
            var result = notifications.Select(n =>
            {
                // We'll use a simple approach since we already materialized the query
                return n;
            }).ToList();

            var count = _unitOfWork.Notifications.Count(n => n.UserId == userId && !n.IsRead);

            return Json(new { count = count, items = notifications }, JsonRequestBehavior.AllowGet);
        }

        // POST: /Notification/MarkAsRead
        [HttpPost]
        public JsonResult MarkAsRead(int id)
        {
            var userId = User.Identity.GetUserId();
            var notification = _unitOfWork.Notifications
                .Find(n => n.NotificationId == id && n.UserId == userId)
                .FirstOrDefault();

            if (notification != null)
            {
                notification.IsRead = true;
                _unitOfWork.SaveChanges();
            }

            return Json(new { success = true });
        }

        // POST: /Notification/MarkAllAsRead
        [HttpPost]
        public JsonResult MarkAllAsRead()
        {
            var userId = User.Identity.GetUserId();
            var notifications = _unitOfWork.Notifications
                .Find(n => n.UserId == userId && !n.IsRead)
                .ToList();

            foreach (var n in notifications)
            {
                n.IsRead = true;
            }
            _unitOfWork.SaveChanges();

            return Json(new { success = true });
        }

        // GET: /Notification/Index
        public ActionResult Index()
        {
            var userId = User.Identity.GetUserId();
            var notifications = _unitOfWork.Notifications
                .Find(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedDate)
                .Take(50)
                .ToList();

            return View(notifications);
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
