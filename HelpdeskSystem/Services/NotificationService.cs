using System;
using System.Linq;
using HelpdeskSystem.Models;
using HelpdeskSystem.Models.Domain;
using HelpdeskSystem.Repositories;
using Microsoft.AspNet.Identity.EntityFramework;

namespace HelpdeskSystem.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;

        public NotificationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        private void CreateNotification(string userId, string title, string message, int ticketId)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Url = "/Ticket/Details/" + ticketId,
                TicketId = ticketId,
                CreatedDate = DateTime.Now,
                IsRead = false
            };
            
            _unitOfWork.Context.Set<Notification>().Add(notification);
        }

        public void NotifyTicketCreated(int ticketId, string ticketCode)
        {
            var adminRoleId = _unitOfWork.Context.Set<IdentityRole>().FirstOrDefault(r => r.Name == "Admin")?.Id;
            if (adminRoleId != null)
            {
                var adminUserIds = _unitOfWork.Context.Set<ApplicationUser>()
                    .Where(u => u.Roles.Any(r => r.RoleId == adminRoleId))
                    .Select(u => u.Id)
                    .ToList();

                foreach (var adminId in adminUserIds)
                {
                    CreateNotification(adminId, "Phiếu yêu cầu mới", $"Phiếu yêu cầu {ticketCode} vừa được tạo.", ticketId);
                }
                
                _unitOfWork.SaveChanges();
            }
        }

        public void NotifyTicketAssigned(int ticketId, string ticketCode, string technicianId)
        {
            CreateNotification(technicianId, "Phân công phiếu yêu cầu", $"Bạn đã được phân công xử lý phiếu {ticketCode}.", ticketId);
            _unitOfWork.SaveChanges();
        }

        public void NotifyTicketStatusChanged(int ticketId, string ticketCode, string newStatus, string createdById)
        {
            CreateNotification(createdById, "Cập nhật trạng thái phiếu", $"Phiếu yêu cầu {ticketCode} đã chuyển sang trạng thái: {newStatus}.", ticketId);
            _unitOfWork.SaveChanges();
        }

        public void NotifyNewComment(int ticketId, string ticketCode, string commentByName, string recipientUserId)
        {
            CreateNotification(recipientUserId, "Bình luận mới", $"{commentByName} đã bình luận về phiếu yêu cầu {ticketCode}.", ticketId);
            _unitOfWork.SaveChanges();
        }

        public void NotifyTicketResolved(int ticketId, string ticketCode, string createdById)
        {
            CreateNotification(createdById, "Phiếu yêu cầu đã giải quyết", $"Phiếu yêu cầu {ticketCode} đã được giải quyết.", ticketId);
            _unitOfWork.SaveChanges();
        }

        public void NotifySLAWarning(int ticketId, string ticketCode, string technicianId)
        {
            CreateNotification(technicianId, "Cảnh báo quá hạn SLA", $"Phiếu yêu cầu {ticketCode} sắp hoặc đã quá hạn xử lý.", ticketId);
            
            var adminRoleId = _unitOfWork.Context.Set<IdentityRole>().FirstOrDefault(r => r.Name == "Admin")?.Id;
            if (adminRoleId != null)
            {
                var adminUserIds = _unitOfWork.Context.Set<ApplicationUser>()
                    .Where(u => u.Roles.Any(r => r.RoleId == adminRoleId))
                    .Select(u => u.Id)
                    .ToList();

                foreach (var adminId in adminUserIds)
                {
                    CreateNotification(adminId, "Cảnh báo quá hạn SLA", $"Phiếu yêu cầu {ticketCode} đang bị quá hạn xử lý.", ticketId);
                }
            }
            
            _unitOfWork.SaveChanges();
        }
    }
}
