using System;
using System.ComponentModel.DataAnnotations;

namespace HelpdeskSystem.Models.Domain
{
    public class TicketHistory
    {
        [Key]
        public int HistoryId { get; set; }

        public int TicketId { get; set; }

        public string ChangedById { get; set; }

        [Required]
        [StringLength(100)]
        public string FieldChanged { get; set; }

        [StringLength(500)]
        public string OldValue { get; set; }

        [StringLength(500)]
        public string NewValue { get; set; }

        public DateTime ChangedDate { get; set; }

        public virtual Ticket Ticket { get; set; }
        public virtual ApplicationUser ChangedBy { get; set; }

        public TicketHistory()
        {
            ChangedDate = DateTime.Now;
        }
    }
}
