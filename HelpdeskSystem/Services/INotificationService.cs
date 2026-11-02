namespace HelpdeskSystem.Services
{
    public interface INotificationService
    {
        void NotifyTicketCreated(int ticketId, string ticketCode);
        void NotifyTicketAssigned(int ticketId, string ticketCode, string technicianId);
        void NotifyTicketStatusChanged(int ticketId, string ticketCode, string newStatus, string createdById);
        void NotifyNewComment(int ticketId, string ticketCode, string commentByName, string recipientUserId);
        void NotifyTicketResolved(int ticketId, string ticketCode, string createdById);
        void NotifySLAWarning(int ticketId, string ticketCode, string technicianId);
    }
}
