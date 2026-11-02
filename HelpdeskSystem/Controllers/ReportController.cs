using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using HelpdeskSystem.Filters;
using HelpdeskSystem.Models;
using HelpdeskSystem.Models.Domain;
using HelpdeskSystem.Models.ViewModels;
using HelpdeskSystem.Repositories;
using OfficeOpenXml;

namespace HelpdeskSystem.Controllers
{
    [AuthorizeRoles("Admin")]
    public class ReportController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReportController()
        {
            _unitOfWork = new UnitOfWork(new ApplicationDbContext());
        }

        // GET: /Report
        public ActionResult Index(DateTime? fromDate, DateTime? toDate, int? categoryId, int? priorityId)
        {
            // Default: last 30 days
            var from = fromDate ?? DateTime.Now.AddDays(-30);
            var to = toDate ?? DateTime.Now;
            to = to.Date.AddDays(1).AddSeconds(-1); // end of day

            var tickets = _unitOfWork.Tickets.GetAll()
                .Include(t => t.Category)
                .Include(t => t.Priority)
                .Include(t => t.CreatedBy)
                .Include(t => t.AssignedTo)
                .Where(t => t.CreatedDate >= from && t.CreatedDate <= to);

            if (categoryId.HasValue)
                tickets = tickets.Where(t => t.CategoryId == categoryId.Value);
            if (priorityId.HasValue)
                tickets = tickets.Where(t => t.PriorityId == priorityId.Value);

            var ticketList = tickets.ToList();

            var model = new ReportViewModel
            {
                FromDate = from,
                ToDate = to,
                CategoryId = categoryId,
                PriorityId = priorityId,

                // Summary stats
                TotalTickets = ticketList.Count,
                ResolvedTickets = ticketList.Count(t => t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed),
                OverdueTickets = ticketList.Count(t => t.IsSLAViolated),
                
                // SLA Compliance
                SLAComplianceRate = ticketList.Count > 0 
                    ? Math.Round((double)ticketList.Count(t => !t.IsSLAViolated) / ticketList.Count * 100, 1) 
                    : 0,

                // Average resolution time (hours)
                AvgResolutionTimeHours = ticketList
                    .Where(t => t.ResolvedDate.HasValue)
                    .Select(t => (t.ResolvedDate.Value - t.CreatedDate).TotalHours)
                    .DefaultIfEmpty(0)
                    .Average(),

                // By category
                TicketsByCategory = ticketList
                    .GroupBy(t => t.Category != null ? t.Category.Name : "Không rõ")
                    .ToDictionary(g => g.Key, g => g.Count()),

                // By priority
                TicketsByPriority = ticketList
                    .GroupBy(t => t.Priority != null ? t.Priority.Name : "Không rõ")
                    .ToDictionary(g => g.Key, g => g.Count()),

                // By status
                TicketsByStatus = ticketList
                    .GroupBy(t => t.Status)
                    .ToDictionary(g => GetStatusName(g.Key), g => g.Count()),

                // Processing time by category
                ProcessingTimeByCategory = ticketList
                    .Where(t => t.ResolvedDate.HasValue && t.Category != null)
                    .GroupBy(t => t.Category.Name)
                    .ToDictionary(
                        g => g.Key,
                        g => Math.Round(g.Average(t => (t.ResolvedDate.Value - t.CreatedDate).TotalHours), 1)
                    ),

                // Staff performance
                StaffPerformance = new List<StaffPerformanceItem>(),

                // Dropdowns
                Categories = new SelectList(_unitOfWork.Categories.GetAll().ToList(), "CategoryId", "Name", categoryId),
                Priorities = new SelectList(_unitOfWork.Priorities.GetAll().ToList(), "PriorityId", "Name", priorityId)
            };

            // Staff performance - query separately
            var staffTickets = ticketList.Where(t => t.AssignedToId != null).ToList();
            var staffGroups = staffTickets.GroupBy(t => t.AssignedToId).ToList();

            var context = _unitOfWork.Context;
            foreach (var group in staffGroups)
            {
                var user = context.Users.Find(group.Key);
                if (user == null) continue;

                var resolvedByStaff = group.Where(t => t.ResolvedDate.HasValue).ToList();
                var avgTime = resolvedByStaff.Any()
                    ? Math.Round(resolvedByStaff.Average(t => (t.ResolvedDate.Value - t.CreatedDate).TotalHours), 1)
                    : 0;

                var ticketIds = group.Select(t => t.TicketId).ToList();
                var ratings = _unitOfWork.TicketRatings.Find(r => ticketIds.Contains(r.TicketId)).ToList();
                var avgRating = ratings.Any() ? Math.Round(ratings.Average(r => r.Stars), 1) : 0;

                model.StaffPerformance.Add(new StaffPerformanceItem
                {
                    TechnicianName = user.FullName,
                    TotalAssigned = group.Count(),
                    TotalResolved = resolvedByStaff.Count,
                    AvgResolutionHours = avgTime,
                    AvgRating = avgRating,
                    SLAComplianceRate = group.Count() > 0
                        ? Math.Round((double)group.Count(t => !t.IsSLAViolated) / group.Count() * 100, 1)
                        : 0
                });
            }

            model.StaffPerformance = model.StaffPerformance.OrderByDescending(s => s.TotalResolved).ToList();

            return View(model);
        }

        // GET: /Report/ExportExcel
        public ActionResult ExportExcel(DateTime? fromDate, DateTime? toDate, int? categoryId, int? priorityId)
        {
            var from = fromDate ?? DateTime.Now.AddDays(-30);
            var to = toDate ?? DateTime.Now;
            to = to.Date.AddDays(1).AddSeconds(-1);

            var tickets = _unitOfWork.Tickets.GetAll()
                .Include(t => t.Category)
                .Include(t => t.Priority)
                .Include(t => t.CreatedBy)
                .Include(t => t.AssignedTo)
                .Where(t => t.CreatedDate >= from && t.CreatedDate <= to);

            if (categoryId.HasValue)
                tickets = tickets.Where(t => t.CategoryId == categoryId.Value);
            if (priorityId.HasValue)
                tickets = tickets.Where(t => t.PriorityId == priorityId.Value);

            var ticketList = tickets.ToList();

            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Báo cáo Ticket");

                // Headers
                ws.Cells[1, 1].Value = "Mã phiếu";
                ws.Cells[1, 2].Value = "Tiêu đề";
                ws.Cells[1, 3].Value = "Danh mục";
                ws.Cells[1, 4].Value = "Ưu tiên";
                ws.Cells[1, 5].Value = "Trạng thái";
                ws.Cells[1, 6].Value = "Người tạo";
                ws.Cells[1, 7].Value = "Người xử lý";
                ws.Cells[1, 8].Value = "Ngày tạo";
                ws.Cells[1, 9].Value = "Ngày xử lý";
                ws.Cells[1, 10].Value = "Thời gian XL (giờ)";
                ws.Cells[1, 11].Value = "SLA";

                // Style header
                using (var range = ws.Cells[1, 1, 1, 11])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(0, 112, 192));
                    range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                }

                // Data
                for (int i = 0; i < ticketList.Count; i++)
                {
                    var t = ticketList[i];
                    var row = i + 2;
                    ws.Cells[row, 1].Value = t.TicketCode;
                    ws.Cells[row, 2].Value = t.Title;
                    ws.Cells[row, 3].Value = t.Category?.Name;
                    ws.Cells[row, 4].Value = t.Priority?.Name;
                    ws.Cells[row, 5].Value = GetStatusName(t.Status);
                    ws.Cells[row, 6].Value = t.CreatedBy?.FullName;
                    ws.Cells[row, 7].Value = t.AssignedTo?.FullName;
                    ws.Cells[row, 8].Value = t.CreatedDate.ToString("dd/MM/yyyy HH:mm");
                    ws.Cells[row, 9].Value = t.ResolvedDate?.ToString("dd/MM/yyyy HH:mm") ?? "";
                    ws.Cells[row, 10].Value = t.ResolvedDate.HasValue
                        ? Math.Round((t.ResolvedDate.Value - t.CreatedDate).TotalHours, 1)
                        : (object)"";
                    ws.Cells[row, 11].Value = t.IsSLAViolated ? "Quá hạn" : "Đúng hạn";
                }

                ws.Cells.AutoFitColumns();

                var fileName = $"BaoCao_Ticket_{from:yyyyMMdd}_{to:yyyyMMdd}.xlsx";
                var content = package.GetAsByteArray();
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        private string GetStatusName(TicketStatus status)
        {
            switch (status)
            {
                case TicketStatus.New: return "Mới tạo";
                case TicketStatus.Assigned: return "Đã phân công";
                case TicketStatus.InProgress: return "Đang xử lý";
                case TicketStatus.Pending: return "Chờ phản hồi";
                case TicketStatus.Resolved: return "Đã giải quyết";
                case TicketStatus.Closed: return "Đã đóng";
                case TicketStatus.Reopened: return "Mở lại";
                default: return status.ToString();
            }
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
