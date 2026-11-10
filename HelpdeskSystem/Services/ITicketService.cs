using System.Collections.Generic;
using HelpdeskSystem.Models;
using HelpdeskSystem.Models.Domain;
using HelpdeskSystem.Models.ViewModels;

namespace HelpdeskSystem.Services
{
    public interface ITicketService
    {
        TicketDetailVM GetTicketDetail(int ticketId, string currentUserId, bool isAdmin, bool isTechnician);
        TicketListVM GetTicketList(TicketFilterVM filter, string currentUserId, bool isAdmin, bool isTechnician);
        Ticket CreateTicket(TicketCreateVM model, string userId);
        void AssignTicket(int ticketId, string technicianId, string assignedBy, int? priorityId = null, int? categoryId = null);
        void ChangeStatus(int ticketId, TicketStatus newStatus, string changedById, string solutionOrNote = null);
        void AddComment(int ticketId, string userId, string content, bool isInternalNote);
        void RateTicket(int ticketId, string userId, int stars, string comment);
        string GetStatusDisplayName(TicketStatus status);
        string GetStatusColor(TicketStatus status);
        List<TicketStatus> GetAllowedTransitions(TicketStatus currentStatus, bool isAdmin, bool isTechnician);
        void CheckAndUpdateSLA(Ticket ticket);
    }
}
