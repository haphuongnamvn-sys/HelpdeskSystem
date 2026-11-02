using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HelpdeskSystem.Helpers;
using HelpdeskSystem.Models;
using HelpdeskSystem.Models.Domain;
using HelpdeskSystem.Repositories;
using HelpdeskSystem.Services;
using Microsoft.AspNet.Identity;

namespace HelpdeskSystem.Controllers
{
    [Authorize]
    public class CommentController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;

        public CommentController()
        {
            var context = new ApplicationDbContext();
            _unitOfWork = new UnitOfWork(context);
            _notificationService = new NotificationService(_unitOfWork);
        }

        // POST: /Comment/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Add(int ticketId, string content, bool isInternalNote = false, HttpPostedFileBase[] attachments = null, string returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = "Nội dung trao đổi không được để trống." });
                }
                TempData["Error"] = "Nội dung trao đổi không được để trống.";
                if (!string.IsNullOrEmpty(returnUrl)) return Redirect(returnUrl);
                return RedirectToAction("Details", "Ticket", new { id = ticketId });
            }

            var userId = User.Identity.GetUserId();
            var userName = User.Identity.Name;
            var isStaff = User.IsInRole("Admin") || User.IsInRole("Technician");

            // Only staff can create internal notes
            if (isInternalNote && !isStaff)
            {
                isInternalNote = false;
            }

            // Create comment
            var comment = new TicketComment
            {
                TicketId = ticketId,
                UserId = userId,
                Content = content,
                IsInternalNote = isInternalNote,
                CreatedDate = DateTime.Now
            };
            _unitOfWork.TicketComments.Add(comment);
            _unitOfWork.SaveChanges();

            // Handle attachments
            if (attachments != null)
            {
                foreach (var file in attachments)
                {
                    if (file != null && file.ContentLength > 0)
                    {
                        var (isValid, errorMsg) = FileHelper.ValidateFile(file);
                        if (isValid)
                        {
                            var (fileName, filePath, fileSize) = FileHelper.SaveFile(file, "comments");
                            var attachment = new TicketAttachment
                            {
                                TicketId = ticketId,
                                CommentId = comment.CommentId,
                                FileName = fileName,
                                FilePath = filePath,
                                FileSize = fileSize,
                                UploadedDate = DateTime.Now
                            };
                            _unitOfWork.TicketAttachments.Add(attachment);
                        }
                    }
                }
                _unitOfWork.SaveChanges();
            }

            // Check if this is the first response from staff
            var ticket = _unitOfWork.Tickets.GetById(ticketId);
            if (ticket != null && isStaff && !ticket.FirstResponseDate.HasValue)
            {
                ticket.FirstResponseDate = DateTime.Now;
                _unitOfWork.SaveChanges();
            }

            // Send notification
            if (ticket != null)
            {
                // Notify the other party
                if (userId == ticket.CreatedById && ticket.AssignedToId != null)
                {
                    _notificationService.NotifyNewComment(ticketId, ticket.TicketCode, userName, ticket.AssignedToId);
                }
                else if (userId != ticket.CreatedById)
                {
                    _notificationService.NotifyNewComment(ticketId, ticket.TicketCode, userName, ticket.CreatedById);
                }
            }

            if (Request.IsAjaxRequest())
            {
                return Json(new { success = true, message = "Đã gửi tin nhắn." });
            }

            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }

            TempData["Success"] = "Đã thêm phản hồi.";
            return RedirectToAction("Details", "Ticket", new { id = ticketId });
        }

        // GET: /Comment/Download/5
        public ActionResult Download(int id)
        {
            var attachment = _unitOfWork.TicketAttachments.GetById(id);
            if (attachment == null)
            {
                return HttpNotFound();
            }

            var physicalPath = Server.MapPath("~" + attachment.FilePath);
            if (!System.IO.File.Exists(physicalPath))
            {
                return HttpNotFound();
            }

            var contentType = FileHelper.GetContentType(attachment.FileName);
            return File(physicalPath, contentType, attachment.FileName);
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
