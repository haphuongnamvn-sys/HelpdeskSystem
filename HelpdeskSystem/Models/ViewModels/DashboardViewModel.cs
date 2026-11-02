using System.Collections.Generic;

namespace HelpdeskSystem.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; }
        public int ResolvedToday { get; set; }
        public int OverdueTickets { get; set; }
        public int UnreadNotifications { get; set; }

        // For charts
        public Dictionary<string, int> TicketsByStatus { get; set; }
        public Dictionary<string, int> TicketsByCategory { get; set; }
        public Dictionary<string, int> TicketsByPriority { get; set; }
        public List<RecentTicketItem> RecentTickets { get; set; }

        public DashboardViewModel()
        {
            TicketsByStatus = new Dictionary<string, int>();
            TicketsByCategory = new Dictionary<string, int>();
            TicketsByPriority = new Dictionary<string, int>();
            RecentTickets = new List<RecentTicketItem>();
        }
    }

    public class RecentTicketItem
    {
        public int TicketId { get; set; }
        public string TicketCode { get; set; }
        public string Title { get; set; }
        public string CategoryName { get; set; }
        public string Status { get; set; }
        public string StatusColor { get; set; }
        public string Priority { get; set; }
        public string PriorityColor { get; set; }
        public string CreatedBy { get; set; }
        public string AssignedTo { get; set; }
        public string CreatedDate { get; set; }
        public string SLAStatus { get; set; }
        public string SLAColor { get; set; }
    }
}
