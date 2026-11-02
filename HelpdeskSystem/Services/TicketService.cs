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
                CategoryName = ticket.Category?.Name,
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
                ResolvedDate = ticket.ResolvedDate,
                ClosedDate = ticket.ClosedDate,
                SLADueDate = ticket.SLADueDate,
                IsSLAViolated = ticket.IsSLAViolated,
                SLAStatusText = ticket.IsSLAViolated ? "Quá hạn" : (ticket.SLADueDate.HasValue ? "Trong hạn" : "N/A"),
                SLAStatusColor = ticket.IsSLAViolated ? "danger" : "success",
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
            vm.CanAssign = isAdmin;
            vm.CanChangeStatus = isAdmin || isTechnician || (ticket.CreatedById == currentUserId && ticket.Status == TicketStatus.Resolved);
            vm.CanComment = true; // Everyone can comment, maybe restricted by status
            if (ticket.Status == TicketStatus.Closed) vm.CanComment = false;
            
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

            // Load technicians for dropdown if admin
            if (isAdmin)
            {
                var roleId = _unitOfWork.Context.Set<Microsoft.AspNet.Identity.EntityFramework.IdentityRole>()
                    .FirstOrDefault(r => r.Name == "Technician")?.Id;

                var techs = _unitOfWork.Context.Set<ApplicationUser>()
                    .Where(u => u.Roles.Any(r => r.RoleId == roleId))
                    .Select(u => new SelectListItem
                    {
                        Value = u.Id,
                        Text = u.FullName ?? u.UserName
                    }).ToList();
                    
                vm.Technicians = techs;
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

            var tickets = query.OrderByDescending(t => t.CreatedDate).ToList();
            
            var vmList = new TicketListVM();
            vmList.Filter = filter;
            
            foreach (var t in tickets)
            {
                CheckAndUpdateSLA(t);
                
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
                    SLAStatus = t.IsSLAViolated ? "Quá hạn" : (t.SLADueDate.HasValue ? "Trong hạn" : "N/A"),
                    SLAStatusColor = t.IsSLAViolated ? "danger" : "success"
                });
            }
            
            _unitOfWork.Context.SaveChanges();

            return vmList;
        }

        public Ticket CreateTicket(TicketCreateVM model, string userId)
        {
            var priority = _unitOfWork.Context.Set<Priority>().Find(model.PriorityId);
            var slaConfig = _unitOfWork.Context.Set<SLAConfig>().FirstOrDefault(s => s.PriorityId == model.PriorityId);
            
            var ticket = new Ticket
            {
                TicketCode = "T-" + DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(10, 99),
                Title = model.Title,
                Description = model.Description,
                CategoryId = model.CategoryId,
                PriorityId = model.PriorityId,
                CreatedById = userId,
                CreatedDate = DateTime.Now,
                Status = TicketStatus.New
            };

            if (slaConfig != null)
            {
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

        public void AssignTicket(int ticketId, string technicianId, string assignedBy)
        {
            var ticket = _unitOfWork.Context.Set<Ticket>().Find(ticketId);
            if (ticket != null)
            {
                string oldAssigned = ticket.AssignedToId;
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
                    OldValue = oldAssigned,
                    NewValue = technicianId
                });

                _unitOfWork.SaveChanges();
            }
        }

        public void ChangeStatus(int ticketId, TicketStatus newStatus, string changedById)
        {
            var ticket = _unitOfWork.Context.Set<Ticket>().Find(ticketId);
            if (ticket != null)
            {
                string oldStatus = ticket.Status.ToString();
                ticket.Status = newStatus;

                if (newStatus == TicketStatus.Resolved)
                {
                    ticket.ResolvedDate = DateTime.Now;
                }
                else if (newStatus == TicketStatus.Closed)
                {
                    ticket.ClosedDate = DateTime.Now;
                }
                else if (newStatus == TicketStatus.Reopened)
                {
                    ticket.ResolvedDate = null;
                    ticket.ClosedDate = null;
                }

                _unitOfWork.Context.Set<TicketHistory>().Add(new TicketHistory
                {
                    TicketId = ticket.TicketId,
                    ChangedById = changedById,
                    ChangedDate = DateTime.Now,
                    FieldChanged = "Trạng thái",
                    OldValue = GetStatusDisplayName((TicketStatus)Enum.Parse(typeof(TicketStatus), oldStatus)),
                    NewValue = GetStatusDisplayName(newStatus)
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
                default: return "secondary";
            }
        }

        public List<TicketStatus> GetAllowedTransitions(TicketStatus currentStatus, bool isAdmin, bool isTechnician)
        {
            var list = new List<TicketStatus>();
            if (isAdmin)
            {
                if (currentStatus == TicketStatus.New) list.Add(TicketStatus.Assigned);
                if (currentStatus == TicketStatus.Resolved) { list.Add(TicketStatus.Closed); list.Add(TicketStatus.Reopened); }
                // Admin can probably do more, but following rules
            }
            if (isTechnician)
            {
                if (currentStatus == TicketStatus.Assigned) list.Add(TicketStatus.InProgress);
                if (currentStatus == TicketStatus.InProgress) { list.Add(TicketStatus.Pending); list.Add(TicketStatus.Resolved); }
                if (currentStatus == TicketStatus.Pending) list.Add(TicketStatus.InProgress);
                if (currentStatus == TicketStatus.Reopened) list.Add(TicketStatus.InProgress);
            }
            if (!isAdmin && !isTechnician)
            {
                // EndUser
                if (currentStatus == TicketStatus.Resolved) list.Add(TicketStatus.Reopened);
            }
            return list;
        }

        public void CheckAndUpdateSLA(Ticket ticket)
        {
            if (ticket.SLADueDate.HasValue && ticket.Status != TicketStatus.Resolved && ticket.Status != TicketStatus.Closed)
            {
                if (DateTime.Now > ticket.SLADueDate.Value)
                {
                    ticket.IsSLAViolated = true;
                }
            }
        }
    }
}
