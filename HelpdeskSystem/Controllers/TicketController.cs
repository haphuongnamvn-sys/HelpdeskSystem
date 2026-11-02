using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HelpdeskSystem.Helpers;
using HelpdeskSystem.Models;
using HelpdeskSystem.Models.Domain;
using HelpdeskSystem.Models.ViewModels;
using HelpdeskSystem.Repositories;
using HelpdeskSystem.Services;
using Microsoft.AspNet.Identity;

namespace HelpdeskSystem.Controllers
{
    [Authorize]
    public class TicketController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITicketService _ticketService;
        private readonly INotificationService _notificationService;

        public TicketController()
        {
            var context = new ApplicationDbContext();
            _unitOfWork = new UnitOfWork(context);
            _ticketService = new TicketService(_unitOfWork);
            _notificationService = new NotificationService(_unitOfWork);
        }

        private bool IsAdmin() => User.IsInRole("Admin");
        private bool IsTechnician() => User.IsInRole("Technician");
        private string CurrentUserId => User.Identity.GetUserId();

        // GET: /Ticket
        public ActionResult Index(TicketFilterVM filter)
        {
            if (filter == null) filter = new TicketFilterVM();

            // Populate dropdown data
            filter.Categories = _unitOfWork.Categories.GetAll()
                .Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.Name })
                .ToList();
            filter.Priorities = _unitOfWork.Priorities.GetAll()
                .Select(p => new SelectListItem { Value = p.PriorityId.ToString(), Text = p.Name })
                .ToList();
            filter.Statuses = Enum.GetValues(typeof(TicketStatus))
                .Cast<TicketStatus>()
                .Select(s => new SelectListItem
                {
                    Value = ((int)s).ToString(),
                    Text = _ticketService.GetStatusDisplayName(s)
                })
                .ToList();

            var vm = _ticketService.GetTicketList(filter, CurrentUserId, IsAdmin(), IsTechnician());
            return View(vm);
        }

        // GET: /Ticket/Create
        [Authorize(Roles = "EndUser")]
        public ActionResult Create()
        {
            var vm = new TicketCreateVM
            {
                Categories = _unitOfWork.Categories.GetAll()
                    .Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.Name })
                    .ToList(),
                Priorities = _unitOfWork.Priorities.GetAll()
                    .Select(p => new SelectListItem { Value = p.PriorityId.ToString(), Text = p.Name })
                    .ToList()
            };
            return View(vm);
        }

        // POST: /Ticket/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "EndUser")]
        public ActionResult Create(TicketCreateVM model)

        {
            if (ModelState.IsValid)
            {
                var ticket = _ticketService.CreateTicket(model, CurrentUserId);

                // Handle file attachments
                if (model.Attachments != null)
                {
                    foreach (var file in model.Attachments)
                    {
                        if (file != null && file.ContentLength > 0)
                        {
                            var (isValid, errorMsg) = FileHelper.ValidateFile(file);
                            if (isValid)
                            {
                                var (fileName, filePath, fileSize) = FileHelper.SaveFile(file, "tickets");
                                var attachment = new TicketAttachment
                                {
                                    TicketId = ticket.TicketId,
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

                _notificationService.NotifyTicketCreated(ticket.TicketId, ticket.TicketCode);

                TempData["Success"] = "Phiếu yêu cầu đã được tạo thành công!";
                return RedirectToAction("Index");
            }

            // Repopulate dropdowns on validation failure
            model.Categories = _unitOfWork.Categories.GetAll()
                .Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.Name })
                .ToList();
            model.Priorities = _unitOfWork.Priorities.GetAll()
                .Select(p => new SelectListItem { Value = p.PriorityId.ToString(), Text = p.Name })
                .ToList();
            return View(model);
        }

        // GET: /Ticket/Details/5
        public ActionResult Details(int id)
        {
            var vm = _ticketService.GetTicketDetail(id, CurrentUserId, IsAdmin(), IsTechnician());
            if (vm == null) return HttpNotFound();

            // Check permissions to view
            if (!IsAdmin() && !IsTechnician() && vm.CreatedById != CurrentUserId)
            {
                return new HttpStatusCodeResult(403, "Bạn không có quyền xem phiếu này.");
            }

            // Populate technician dropdown for assignment
            if (IsAdmin())
            {
                vm.Technicians = _unitOfWork.Context.Users
                    .Where(u => u.IsActive)
                    .ToList()
                    .Where(u =>
                    {
                        var roles = _unitOfWork.Context.Set<Microsoft.AspNet.Identity.EntityFramework.IdentityUserRole>()
                            .Where(r => r.UserId == u.Id).Select(r => r.RoleId).ToList();
                        var techRoleId = _unitOfWork.Context.Set<Microsoft.AspNet.Identity.EntityFramework.IdentityRole>()
                            .Where(r => r.Name == "Technician").Select(r => r.Id).FirstOrDefault();
                        return roles.Contains(techRoleId);
                    })
                    .Select(u => new SelectListItem
                    {
                        Value = u.Id,
                        Text = u.FullName + " (" + (u.Department != null ? u.Department.Name : "N/A") + ")"
                    })
                    .ToList();
            }

            return View(vm);
        }

        // GET: /Ticket/Chat/5
        public ActionResult Chat(int id)
        {
            var vm = _ticketService.GetTicketDetail(id, CurrentUserId, IsAdmin(), IsTechnician());
            if (vm == null) return HttpNotFound();

            // Check permissions to view
            if (!IsAdmin() && !IsTechnician() && vm.CreatedById != CurrentUserId)
            {
                return new HttpStatusCodeResult(403, "Bạn không có quyền xem phiếu này.");
            }

            return View(vm);
        }

        // POST: /Ticket/Assign
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Assign(int ticketId, string technicianId)
        {
            if (string.IsNullOrEmpty(technicianId))
            {
                TempData["Error"] = "Vui lòng chọn nhân viên xử lý.";
                return RedirectToAction("Details", new { id = ticketId });
            }

            _ticketService.AssignTicket(ticketId, technicianId, CurrentUserId);

            var ticket = _unitOfWork.Tickets.GetById(ticketId);
            _notificationService.NotifyTicketAssigned(ticketId, ticket.TicketCode, technicianId);

            TempData["Success"] = "Đã phân công nhân viên xử lý.";
            return RedirectToAction("Details", new { id = ticketId });
        }

        // POST: /Ticket/ChangeStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangeStatus(int ticketId, TicketStatus newStatus)
        {
            var ticket = _unitOfWork.Tickets.GetById(ticketId);
            if (ticket == null) return HttpNotFound();

            // Validate permissions
            var allowedTransitions = _ticketService.GetAllowedTransitions(ticket.Status, IsAdmin(), IsTechnician());
            if (!allowedTransitions.Contains(newStatus))
            {
                TempData["Error"] = "Không thể chuyển sang trạng thái này.";
                return RedirectToAction("Details", new { id = ticketId });
            }

            // EndUser can only reopen (Resolved → Reopened)
            if (!IsAdmin() && !IsTechnician() && newStatus != TicketStatus.Reopened)
            {
                TempData["Error"] = "Bạn không có quyền thực hiện thao tác này.";
                return RedirectToAction("Details", new { id = ticketId });
            }

            _ticketService.ChangeStatus(ticketId, newStatus, CurrentUserId);

            var statusName = _ticketService.GetStatusDisplayName(newStatus);
            _notificationService.NotifyTicketStatusChanged(ticketId, ticket.TicketCode, statusName, ticket.CreatedById);

            if (newStatus == TicketStatus.Resolved)
            {
                _notificationService.NotifyTicketResolved(ticketId, ticket.TicketCode, ticket.CreatedById);
            }

            TempData["Success"] = $"Đã cập nhật trạng thái: {statusName}";
            return RedirectToAction("Details", new { id = ticketId });
        }

        // POST: /Ticket/Rate
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "EndUser")]
        public ActionResult Rate(int ticketId, int stars, string comment)
        {
            _ticketService.RateTicket(ticketId, CurrentUserId, stars, comment);
            TempData["Success"] = "Cảm ơn bạn đã đánh giá!";
            return RedirectToAction("Details", new { id = ticketId });
        }

        // GET: /Ticket/Edit/5
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(int id)
        {
            var ticket = _unitOfWork.Tickets.GetById(id);
            if (ticket == null) return HttpNotFound();

            var vm = new TicketEditVM
            {
                TicketId = ticket.TicketId,
                TicketCode = ticket.TicketCode,
                Title = ticket.Title,
                Description = ticket.Description,
                CategoryId = ticket.CategoryId,
                PriorityId = ticket.PriorityId,
                Categories = _unitOfWork.Categories.GetAll()
                    .Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.Name })
                    .ToList(),
                Priorities = _unitOfWork.Priorities.GetAll()
                    .Select(p => new SelectListItem { Value = p.PriorityId.ToString(), Text = p.Name })
                    .ToList()
            };
            return View(vm);
        }

        // POST: /Ticket/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(TicketEditVM model)
        {
            if (ModelState.IsValid)
            {
                var ticket = _unitOfWork.Tickets.GetById(model.TicketId);
                if (ticket == null) return HttpNotFound();

                // Log changes
                if (ticket.Title != model.Title)
                {
                    _unitOfWork.TicketHistories.Add(new TicketHistory
                    {
                        TicketId = ticket.TicketId,
                        ChangedById = CurrentUserId,
                        FieldChanged = "Tiêu đề",
                        OldValue = ticket.Title,
                        NewValue = model.Title,
                        ChangedDate = DateTime.Now
                    });
                    ticket.Title = model.Title;
                }

                if (ticket.CategoryId != model.CategoryId)
                {
                    var oldCat = _unitOfWork.Categories.GetById(ticket.CategoryId);
                    var newCat = _unitOfWork.Categories.GetById(model.CategoryId);
                    _unitOfWork.TicketHistories.Add(new TicketHistory
                    {
                        TicketId = ticket.TicketId,
                        ChangedById = CurrentUserId,
                        FieldChanged = "Danh mục",
                        OldValue = oldCat?.Name,
                        NewValue = newCat?.Name,
                        ChangedDate = DateTime.Now
                    });
                    ticket.CategoryId = model.CategoryId;
                }

                if (ticket.PriorityId != model.PriorityId)
                {
                    var oldPri = _unitOfWork.Priorities.GetById(ticket.PriorityId);
                    var newPri = _unitOfWork.Priorities.GetById(model.PriorityId);
                    _unitOfWork.TicketHistories.Add(new TicketHistory
                    {
                        TicketId = ticket.TicketId,
                        ChangedById = CurrentUserId,
                        FieldChanged = "Mức ưu tiên",
                        OldValue = oldPri?.Name,
                        NewValue = newPri?.Name,
                        ChangedDate = DateTime.Now
                    });
                    ticket.PriorityId = model.PriorityId;

                    // Recalculate SLA due date
                    var slaConfig = _unitOfWork.SLAConfigs.Find(s => s.PriorityId == model.PriorityId).FirstOrDefault();
                    if (slaConfig != null)
                    {
                        ticket.SLADueDate = ticket.CreatedDate.AddHours(slaConfig.ResolutionTimeHours);
                        ticket.IsSLAViolated = DateTime.Now > ticket.SLADueDate;
                    }
                }

                ticket.Description = model.Description;
                _unitOfWork.SaveChanges();

                TempData["Success"] = "Đã cập nhật phiếu yêu cầu.";
                return RedirectToAction("Details", new { id = model.TicketId });
            }

            model.Categories = _unitOfWork.Categories.GetAll()
                .Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.Name })
                .ToList();
            model.Priorities = _unitOfWork.Priorities.GetAll()
                .Select(p => new SelectListItem { Value = p.PriorityId.ToString(), Text = p.Name })
                .ToList();
            return View(model);
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
