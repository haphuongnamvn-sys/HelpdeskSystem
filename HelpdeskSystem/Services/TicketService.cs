using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using HelpdeskSystem.Helpers;
using HelpdeskSystem.Models;
using HelpdeskSystem.Models.Domain;
using HelpdeskSystem.Models.ViewModels;
using HelpdeskSystem.Repositories;

namespace HelpdeskSystem.Services
{
    public class TicketService : ITicketService
    {
        private readonly IUnitOfWork _unitOfWork;

        public TicketService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public TicketDetailVM GetTicketDetail(int ticketId, string currentUserId, bool isAdmin, bool isTechnician)
        {
            var ticket = _unitOfWork.Context.Set<Ticket>()
                .Include(t => t.Category)
                .Include(t => t.Priority)
                .Include(t => t.CreatedBy)
                .Include(t => t.AssignedTo)
                .FirstOrDefault(t => t.TicketId == ticketId);

            if (ticket == null) return null;

            CheckAndUpdateSLA(ticket);
            _unitOfWork.Context.SaveChanges();

            var comments = _unitOfWork.Context.Set<TicketComment>()
                .Include(c => c.User)
                .Include(c => c.Attachments)
                .Where(c => c.TicketId == ticketId)
                .OrderBy(c => c.CreatedDate)
                .ToList();

            var history = _unitOfWork.Context.Set<TicketHistory>()
                .Include(h => h.ChangedBy)
                .Where(h => h.TicketId == ticketId)
                .OrderByDescending(h => h.ChangedDate)
                .ToList();

            var attachments = _unitOfWork.Context.Set<TicketAttachment>()
                .Where(a => a.TicketId == ticketId && a.CommentId == null)
                .ToList();

            var rating = _unitOfWork.Context.Set<TicketRating>()
                .FirstOrDefault(r => r.TicketId == ticketId);

            var vm = new TicketDetailVM
            {
                TicketId = ticket.TicketId,
                TicketCode = ticket.TicketCode,
                Title = ticket.Title,
                Description = ticket.Description,
                CategoryId = ticket.CategoryId,
                CategoryName = ticket.Category?.Name,
                PriorityId = ticket.PriorityId,
                PriorityName = ticket.Priority?.Name,
                PriorityColor = ticket.Priority?.ColorCode ?? "secondary",
                Status = ticket.Status,
                StatusDisplay = GetStatusDisplayName(ticket.Status),
                StatusColor = GetStatusColor(ticket.Status),
                CreatedByName = ticket.CreatedBy?.FullName ?? ticket.CreatedBy?.UserName,
                CreatedById = ticket.CreatedById,
                AssignedToName = ticket.AssignedTo?.FullName ?? ticket.AssignedTo?.UserName,
                AssignedToId = ticket.AssignedToId,
                CreatedDate = ticket.CreatedDate,
                AssignedDate = ticket.AssignedDate,
                ReceivedDate = ticket.ReceivedDate,
                ResolvedDate = ticket.ResolvedDate,
                ClosedDate = ticket.ClosedDate,
                ResponseDueDate = ticket.ResponseDueDate,
                SLADueDate = ticket.SLADueDate,
                IsSLAViolated = ticket.IsSLAViolated,
                SLAStatusText = ticket.IsSLAViolated ? "Quá hạn" : (ticket.SLADueDate.HasValue ? "Trong hạn" : "N/A"),
                SLAStatusColor = ticket.IsSLAViolated ? "danger" : "success",
                Solution = ticket.Solution,
                SelectedTechnicianId = ticket.AssignedToId
            };

            // Map comments
            foreach (var c in comments)
            {
                if (c.IsInternalNote && !isAdmin && !isTechnician)
                    continue;

                vm.Comments.Add(new CommentItem
                {
                    CommentId = c.CommentId,
                    UserId = c.UserId,
                    UserName = c.User?.FullName ?? c.User?.UserName,
                    UserRole = c.User?.Id == ticket.CreatedById ? "Người yêu cầu" : "Nhân viên",
                    Content = c.Content,
                    IsInternalNote = c.IsInternalNote,
                    CreatedDate = c.CreatedDate,
                    Attachments = c.Attachments.Select(a => new AttachmentItem
                    {
                        AttachmentId = a.AttachmentId,
                        FileName = a.FileName,
                        FilePath = a.FilePath,
                        FileSize = FileHelper.FormatFileSize(a.FileSize),
                        UploadedDate = a.UploadedDate
                    }).ToList()
                });
            }

            // Map history
            foreach (var h in history)
            {
                vm.History.Add(new HistoryItem
                {
                    ChangedByName = h.ChangedBy?.FullName ?? h.ChangedBy?.UserName,
                    FieldChanged = h.FieldChanged,
                    OldValue = h.OldValue,
                    NewValue = h.NewValue,
                    ChangedDate = h.ChangedDate
                });
            }

            // Map attachments
            foreach (var a in attachments)
            {
                vm.Attachments.Add(new AttachmentItem
                {
                    AttachmentId = a.AttachmentId,
                    FileName = a.FileName,
                    FilePath = a.FilePath,
                    FileSize = FileHelper.FormatFileSize(a.FileSize),
                    UploadedDate = a.UploadedDate
                });
            }

            // Permissions
            vm.CanAssign = isAdmin && ticket.Status != TicketStatus.Closed && ticket.Status != TicketStatus.Cancelled;
            vm.CanCancel = (ticket.CreatedById == currentUserId &&
                           (ticket.Status == TicketStatus.New || (ticket.Status == TicketStatus.Assigned && !ticket.ReceivedDate.HasValue)));
            vm.CanConfirmResult = (ticket.CreatedById == currentUserId && ticket.Status == TicketStatus.Resolved);
            vm.CanChangeStatus = isAdmin || isTechnician || vm.CanCancel || vm.CanConfirmResult;
            vm.CanComment = (ticket.Status != TicketStatus.Closed && ticket.Status != TicketStatus.Cancelled);

            vm.CanAddInternalNote = isAdmin || isTechnician;
            vm.AllowedTransitions = GetAllowedTransitions(ticket.Status, isAdmin, isTechnician);

            // Rating logic
            vm.HasRated = rating != null;
            if (rating != null)
            {
                vm.RatingStars = rating.Stars;
                vm.RatingComment = rating.Comment;
            }

            vm.CanRate = (!vm.HasRated && ticket.CreatedById == currentUserId &&
                         (ticket.Status == TicketStatus.Resolved || ticket.Status == TicketStatus.Closed));

            // Load technicians, priorities, categories for dropdown if admin/manager
            if (isAdmin)
            {
                var roleId = _unitOfWork.Context.Set<Microsoft.AspNet.Identity.EntityFramework.IdentityRole>()
                    .FirstOrDefault(r => r.Name == "Technician")?.Id;

                var techs = _unitOfWork.Context.Set<ApplicationUser>()
                    .Where(u => u.IsActive && u.Roles.Any(r => r.RoleId == roleId))
                    .Select(u => new SelectListItem
                    {
                        Value = u.Id,
                        Text = u.FullName ?? u.UserName
                    }).ToList();

                vm.Technicians = techs;
                vm.Priorities = _unitOfWork.Priorities.GetAll()
                    .OrderBy(p => p.Level)
                    .Select(p => new SelectListItem
                    {
                        Value = p.PriorityId.ToString(),
                        Text = p.Name,
                        Selected = p.PriorityId == ticket.PriorityId
                    }).ToList();
                vm.Categories = _unitOfWork.Categories.GetAll()
                    .Where(c => c.IsActive || c.CategoryId == ticket.CategoryId)
                    .Select(c => new SelectListItem
                    {
                        Value = c.CategoryId.ToString(),
                        Text = c.Name,
                        Selected = c.CategoryId == ticket.CategoryId
                    }).ToList();
            }

            return vm;
        }

        public TicketListVM GetTicketList(TicketFilterVM filter, string currentUserId, bool isAdmin, bool isTechnician)
        {
            var query = _unitOfWork.Context.Set<Ticket>()
                .Include(t => t.Category)
                .Include(t => t.Priority)
                .Include(t => t.CreatedBy)
                .Include(t => t.AssignedTo)
                .AsQueryable();

            if (!isAdmin)
            {
                if (isTechnician)
                {
                    query = query.Where(t => t.AssignedToId == currentUserId || t.CreatedById == currentUserId);
                }
                else
                {
                    query = query.Where(t => t.CreatedById == currentUserId);
                }
            }

            if (filter.Status.HasValue)
                query = query.Where(t => t.Status == filter.Status.Value);

            if (filter.CategoryId.HasValue)
                query = query.Where(t => t.CategoryId == filter.CategoryId.Value);

            if (filter.PriorityId.HasValue)
                query = query.Where(t => t.PriorityId == filter.PriorityId.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(t => t.CreatedDate >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
            {
                var toDate = filter.ToDate.Value.AddDays(1);
                query = query.Where(t => t.CreatedDate < toDate);
            }

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var search = filter.SearchTerm.ToLower();
                query = query.Where(t => t.Title.ToLower().Contains(search) || t.TicketCode.ToLower().Contains(search));
            }

            if (!string.IsNullOrEmpty(filter.QuickFilter))
            {
                var now = DateTime.Now;
                var warningSoon = now.AddHours(4);
                switch (filter.QuickFilter.ToLower())
                {
                    case "unassigned":
                        query = query.Where(t => t.AssignedToId == null && t.Status == TicketStatus.New);
                        break;
                    case "duesoon":
                        query = query.Where(t => t.SLADueDate.HasValue && t.SLADueDate.Value > now && t.SLADueDate.Value <= warningSoon && t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed && t.Status != TicketStatus.Cancelled);
                        break;
                    case "overdue":
                        query = query.Where(t => (t.IsSLAViolated || (t.SLADueDate.HasValue && t.SLADueDate.Value < now)) && t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed && t.Status != TicketStatus.Cancelled);
                        break;
                }
            }

            var allTickets = query.OrderByDescending(t => t.CreatedDate).ToList();

            foreach (var t in allTickets)
            {
                CheckAndUpdateSLA(t);
            }
            _unitOfWork.Context.SaveChanges();

            // Pagination: 10 tickets per page as specified in Baocao_DA (UC03 - 2.6.3)
            int pageSize = 10;
            int page = filter.Page > 0 ? filter.Page : 1;
            int totalItems = allTickets.Count;
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page > totalPages) page = totalPages;

            filter.Page = page;
            filter.TotalPages = totalPages;
            filter.TotalItems = totalItems;

            var pagedTickets = allTickets.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var vmList = new TicketListVM();
            vmList.Filter = filter;

            foreach (var t in pagedTickets)
            {
                string slaText = "N/A";
                string slaColor = "secondary";
                if (t.Status == TicketStatus.Cancelled)
                {
                    slaText = "Đã hủy";
                    slaColor = "secondary";
                }
                else if (t.IsSLAViolated)
                {
                    slaText = "Quá hạn";
                    slaColor = "danger";
                }
                else if (t.SLADueDate.HasValue)
                {
                    if (t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed && t.SLADueDate.Value <= DateTime.Now.AddHours(4))
                    {
                        slaText = "Sắp đến hạn";
                        slaColor = "warning";
                    }
                    else
                    {
                        slaText = "Trong hạn";
                        slaColor = "success";
                    }
                }

                vmList.Tickets.Add(new TicketListItem
                {
                    TicketId = t.TicketId,
                    TicketCode = t.TicketCode,
                    Title = t.Title,
                    Description = t.Description,
                    CategoryName = t.Category?.Name,
                    PriorityName = t.Priority?.Name,
                    PriorityColor = t.Priority?.ColorCode ?? "secondary",
                    StatusDisplay = GetStatusDisplayName(t.Status),
                    StatusColor = GetStatusColor(t.Status),
                    CreatedByName = t.CreatedBy?.FullName ?? t.CreatedBy?.UserName,
                    AssignedToName = t.AssignedTo?.FullName ?? t.AssignedTo?.UserName,
                    CreatedDate = t.CreatedDate.ToString("dd/MM/yyyy HH:mm"),
                    SLAStatus = slaText,
                    SLAStatusColor = slaColor
                });
            }

            return vmList;
        }

        public Ticket CreateTicket(TicketCreateVM model, string userId)
        {
            int priorityId = model.PriorityId > 0 ? model.PriorityId : 3; // Default: Trung bình (Level 3)
            var slaConfig = _unitOfWork.Context.Set<SLAConfig>().FirstOrDefault(s => s.PriorityId == priorityId);

            var ticket = new Ticket
            {
                TicketCode = "T-" + DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(10, 99),
                Title = model.Title,
                Description = model.Description,
                CategoryId = model.CategoryId,
                PriorityId = priorityId,
                CreatedById = userId,
                CreatedDate = DateTime.Now,
                Status = TicketStatus.New
            };

            if (slaConfig != null)
            {
                ticket.ResponseDueDate = DateTime.Now.AddHours(slaConfig.ResponseTimeHours);
                ticket.SLADueDate = DateTime.Now.AddHours(slaConfig.ResolutionTimeHours);
            }

            _unitOfWork.Context.Set<Ticket>().Add(ticket);

            var history = new TicketHistory
            {
                TicketId = ticket.TicketId,
                Ticket = ticket,
                ChangedById = userId,
                ChangedDate = DateTime.Now,
                FieldChanged = "Trạng thái",
                NewValue = "Tạo mới"
            };

            _unitOfWork.Context.Set<TicketHistory>().Add(history);
            _unitOfWork.SaveChanges();

            return ticket;
        }

        public void AssignTicket(int ticketId, string technicianId, string assignedBy, int? priorityId = null, int? categoryId = null)
        {
            var ticket = _unitOfWork.Context.Set<Ticket>().Find(ticketId);
            if (ticket != null)
            {
                if (categoryId.HasValue && categoryId.Value > 0 && ticket.CategoryId != categoryId.Value)
                {
                    var oldCat = _unitOfWork.Categories.GetById(ticket.CategoryId);
                    var newCat = _unitOfWork.Categories.GetById(categoryId.Value);
                    ticket.CategoryId = categoryId.Value;
                    _unitOfWork.Context.Set<TicketHistory>().Add(new TicketHistory
                    {
                        TicketId = ticket.TicketId,
                        ChangedById = assignedBy,
                        ChangedDate = DateTime.Now,
                        FieldChanged = "Danh mục dịch vụ",
                        OldValue = oldCat?.Name,
                        NewValue = newCat?.Name
                    });
                }

                if (priorityId.HasValue && priorityId.Value > 0 && ticket.PriorityId != priorityId.Value)
                {
                    var oldPri = _unitOfWork.Priorities.GetById(ticket.PriorityId);
                    var newPri = _unitOfWork.Priorities.GetById(priorityId.Value);
                    ticket.PriorityId = priorityId.Value;

                    var slaConfig = _unitOfWork.Context.Set<SLAConfig>().FirstOrDefault(s => s.PriorityId == priorityId.Value);
                    if (slaConfig != null)
                    {
                        ticket.ResponseDueDate = ticket.CreatedDate.AddHours(slaConfig.ResponseTimeHours);
                        ticket.SLADueDate = ticket.CreatedDate.AddHours(slaConfig.ResolutionTimeHours);
                        ticket.IsSLAViolated = DateTime.Now > ticket.SLADueDate.Value;
                    }

                    _unitOfWork.Context.Set<TicketHistory>().Add(new TicketHistory
                    {
                        TicketId = ticket.TicketId,
                        ChangedById = assignedBy,
                        ChangedDate = DateTime.Now,
                        FieldChanged = "Mức ưu tiên",
                        OldValue = oldPri?.Name,
                        NewValue = newPri?.Name
                    });
                }

                var oldTech = !string.IsNullOrEmpty(ticket.AssignedToId) ? _unitOfWork.Context.Users.Find(ticket.AssignedToId) : null;
                var newTech = _unitOfWork.Context.Users.Find(technicianId);

                ticket.AssignedToId = technicianId;
                ticket.AssignedDate = DateTime.Now;

                if (ticket.Status == TicketStatus.New)
                {
                    ticket.Status = TicketStatus.Assigned;
                }

                _unitOfWork.Context.Set<TicketHistory>().Add(new TicketHistory
                {
                    TicketId = ticket.TicketId,
                    ChangedById = assignedBy,
                    ChangedDate = DateTime.Now,
                    FieldChanged = "Phân công",
                    OldValue = oldTech != null ? oldTech.FullName : "Chưa phân công",
                    NewValue = newTech != null ? newTech.FullName : technicianId
                });

                _unitOfWork.SaveChanges();
            }
        }

        public void ChangeStatus(int ticketId, TicketStatus newStatus, string changedById, string solutionOrNote = null)
        {
            var ticket = _unitOfWork.Context.Set<Ticket>().Find(ticketId);
            if (ticket != null)
            {
                string oldStatus = ticket.Status.ToString();
                ticket.Status = newStatus;

                if (newStatus == TicketStatus.InProgress && !ticket.ReceivedDate.HasValue)
                {
                    ticket.ReceivedDate = DateTime.Now;
                }
                else if (newStatus == TicketStatus.Resolved)
                {
                    ticket.ResolvedDate = DateTime.Now;
                    if (!string.IsNullOrWhiteSpace(solutionOrNote))
                    {
                        ticket.Solution = solutionOrNote.Trim();
                    }
                }
                else if (newStatus == TicketStatus.Closed || newStatus == TicketStatus.Cancelled)
                {
                    ticket.ClosedDate = DateTime.Now;
                }
                else if (newStatus == TicketStatus.Reopened)
                {
                    ticket.ResolvedDate = null;
                    ticket.ClosedDate = null;
                }

                string newValueLog = GetStatusDisplayName(newStatus);
                if (!string.IsNullOrWhiteSpace(solutionOrNote))
                {
                    newValueLog += " — Ghi chú/Giải pháp: " + solutionOrNote.Trim();
                }

                _unitOfWork.Context.Set<TicketHistory>().Add(new TicketHistory
                {
                    TicketId = ticket.TicketId,
                    ChangedById = changedById,
                    ChangedDate = DateTime.Now,
                    FieldChanged = "Trạng thái",
                    OldValue = GetStatusDisplayName((TicketStatus)Enum.Parse(typeof(TicketStatus), oldStatus)),
                    NewValue = newValueLog
                });

                _unitOfWork.SaveChanges();
            }
        }

        public void AddComment(int ticketId, string userId, string content, bool isInternalNote)
        {
            var ticket = _unitOfWork.Context.Set<Ticket>().Find(ticketId);
            if (ticket != null)
            {
                var comment = new TicketComment
                {
                    TicketId = ticketId,
                    UserId = userId,
                    Content = content,
                    IsInternalNote = isInternalNote,
                    CreatedDate = DateTime.Now
                };

                _unitOfWork.Context.Set<TicketComment>().Add(comment);

                // Check for first response from staff
                if (ticket.CreatedById != userId && !ticket.FirstResponseDate.HasValue && !isInternalNote)
                {
                    ticket.FirstResponseDate = DateTime.Now;
                }

                _unitOfWork.SaveChanges();
            }
        }

        public void RateTicket(int ticketId, string userId, int stars, string comment)
        {
            var ticket = _unitOfWork.Context.Set<Ticket>().Find(ticketId);
            if (ticket != null && ticket.CreatedById == userId)
            {
                var existing = _unitOfWork.Context.Set<TicketRating>().FirstOrDefault(r => r.TicketId == ticketId);
                if (existing != null) return;

                var rating = new TicketRating
                {
                    TicketId = ticketId,
                    UserId = userId,
                    Stars = stars,
                    Comment = comment,
                    RatedDate = DateTime.Now
                };

                _unitOfWork.Context.Set<TicketRating>().Add(rating);
                _unitOfWork.SaveChanges();
            }
        }

        public string GetStatusDisplayName(TicketStatus status)
        {
            switch (status)
            {
                case TicketStatus.New: return "Mới";
                case TicketStatus.Assigned: return "Đã phân công";
                case TicketStatus.InProgress: return "Đang xử lý";
                case TicketStatus.Pending: return "Chờ phản hồi";
                case TicketStatus.Resolved: return "Đã giải quyết";
                case TicketStatus.Closed: return "Đã đóng";
                case TicketStatus.Reopened: return "Mở lại";
                case TicketStatus.Cancelled: return "Đã hủy";
                default: return status.ToString();
            }
        }

        public string GetStatusColor(TicketStatus status)
        {
            switch (status)
            {
                case TicketStatus.New: return "info";
                case TicketStatus.Assigned: return "primary";
                case TicketStatus.InProgress: return "warning";
                case TicketStatus.Pending: return "secondary";
                case TicketStatus.Resolved: return "success";
                case TicketStatus.Closed: return "dark";
                case TicketStatus.Reopened: return "danger";
                case TicketStatus.Cancelled: return "secondary";
                default: return "secondary";
            }
        }

        public List<TicketStatus> GetAllowedTransitions(TicketStatus currentStatus, bool isAdmin, bool isTechnician)
        {
            var list = new List<TicketStatus>();
            if (isAdmin)
            {
                if (currentStatus == TicketStatus.New) { list.Add(TicketStatus.Assigned); list.Add(TicketStatus.Cancelled); }
                if (currentStatus == TicketStatus.Assigned) { list.Add(TicketStatus.InProgress); list.Add(TicketStatus.Cancelled); }
                if (currentStatus == TicketStatus.InProgress) { list.Add(TicketStatus.Pending); list.Add(TicketStatus.Resolved); }
                if (currentStatus == TicketStatus.Pending) { list.Add(TicketStatus.InProgress); list.Add(TicketStatus.Resolved); }
                if (currentStatus == TicketStatus.Resolved) { list.Add(TicketStatus.Closed); list.Add(TicketStatus.Reopened); }
                if (currentStatus == TicketStatus.Reopened) { list.Add(TicketStatus.InProgress); list.Add(TicketStatus.Resolved); }
            }
            if (isTechnician)
            {
                if (currentStatus == TicketStatus.Assigned && !list.Contains(TicketStatus.InProgress)) list.Add(TicketStatus.InProgress);
                if (currentStatus == TicketStatus.InProgress)
                {
                    if (!list.Contains(TicketStatus.Pending)) list.Add(TicketStatus.Pending);
                    if (!list.Contains(TicketStatus.Resolved)) list.Add(TicketStatus.Resolved);
                }
                if (currentStatus == TicketStatus.Pending && !list.Contains(TicketStatus.InProgress)) list.Add(TicketStatus.InProgress);
                if (currentStatus == TicketStatus.Reopened && !list.Contains(TicketStatus.InProgress)) list.Add(TicketStatus.InProgress);
            }
            if (!isAdmin && !isTechnician)
            {
                // EndUser transitions per Baocao_DA (UC03 - 2.5.2)
                if (currentStatus == TicketStatus.New || currentStatus == TicketStatus.Assigned)
                {
                    list.Add(TicketStatus.Cancelled);
                }
                if (currentStatus == TicketStatus.Resolved)
                {
                    list.Add(TicketStatus.Closed);
                    list.Add(TicketStatus.Reopened);
                }
            }
            return list;
        }

        public void CheckAndUpdateSLA(Ticket ticket)
        {
            if (ticket.SLADueDate.HasValue &&
                ticket.Status != TicketStatus.Resolved &&
                ticket.Status != TicketStatus.Closed &&
                ticket.Status != TicketStatus.Cancelled)
            {
                if (DateTime.Now > ticket.SLADueDate.Value)
                {
                    ticket.IsSLAViolated = true;
                }
            }
        }
    }
}
