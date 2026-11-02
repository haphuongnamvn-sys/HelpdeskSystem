using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using HelpdeskSystem.Models;
using HelpdeskSystem.Models.Domain;
using HelpdeskSystem.Models.ViewModels;
using HelpdeskSystem.Repositories;
using HelpdeskSystem.Services;
using Microsoft.AspNet.Identity;

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

        public ActionResult Index()
        {
            // EndUser goes straight to their ticket list
            if (User.IsInRole("EndUser"))
            {
                return RedirectToAction("Index", "Ticket");
            }

            var userId = User.Identity.GetUserId();
            var isAdmin = User.IsInRole("Admin");

            var ticketsQuery = _unitOfWork.Context.Tickets
                .Include(t => t.Category)
                .Include(t => t.Priority)
                .Include(t => t.CreatedBy)
                .Include(t => t.AssignedTo)
                .AsQueryable();

            // Technician only sees their assigned tickets
            if (!isAdmin)
            {
                ticketsQuery = ticketsQuery.Where(t => t.AssignedToId == userId);
            }

            var allTickets = ticketsQuery.ToList();
            var today = DateTime.Today;

            var model = new DashboardViewModel
            {
                TotalTickets = allTickets.Count,
                OpenTickets = allTickets.Count(t =>
                    t.Status == TicketStatus.New ||
                    t.Status == TicketStatus.Assigned ||
                    t.Status == TicketStatus.InProgress ||
                    t.Status == TicketStatus.Pending ||
                    t.Status == TicketStatus.Reopened),
                ResolvedToday = allTickets.Count(t =>
                    t.ResolvedDate.HasValue && t.ResolvedDate.Value.Date == today),
                OverdueTickets = allTickets.Count(t =>
                    t.IsSLAViolated && t.Status != TicketStatus.Closed),
                UnreadNotifications = _unitOfWork.Notifications
                    .Count(n => n.UserId == userId && !n.IsRead)
            };

            // Tickets by Status
            var statusGroups = allTickets.GroupBy(t => t.Status);
            foreach (var group in statusGroups)
            {
                model.TicketsByStatus[_ticketService.GetStatusDisplayName(group.Key)] = group.Count();
            }

            // Tickets by Category
            var categoryGroups = allTickets.GroupBy(t => t.Category?.Name ?? "Khác");
            foreach (var group in categoryGroups)
            {
                model.TicketsByCategory[group.Key] = group.Count();
            }

            // Tickets by Priority
            var priorityGroups = allTickets.GroupBy(t => t.Priority?.Name ?? "N/A");
            foreach (var group in priorityGroups)
            {
                model.TicketsByPriority[group.Key] = group.Count();
            }

            // Recent 10 tickets
            model.RecentTickets = allTickets
                .OrderByDescending(t => t.CreatedDate)
                .Take(10)
                .Select(t => new RecentTicketItem
                {
                    TicketId = t.TicketId,
                    TicketCode = t.TicketCode,
                    Title = t.Title,
                    CategoryName = t.Category?.Name ?? "Chung",
                    Status = _ticketService.GetStatusDisplayName(t.Status),
                    StatusColor = _ticketService.GetStatusColor(t.Status),
                    Priority = t.Priority?.Name ?? "N/A",
                    PriorityColor = t.Priority?.ColorCode ?? "#6c757d",
                    CreatedBy = t.CreatedBy?.FullName ?? "N/A",
                    AssignedTo = t.AssignedTo?.FullName ?? "Chưa phân công",
                    CreatedDate = t.CreatedDate.ToString("dd/MM/yyyy HH:mm"),
                    SLAStatus = t.IsSLAViolated ? "Quá hạn" : "Đúng hạn",
                    SLAColor = t.IsSLAViolated ? "danger" : "success"
                })
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
