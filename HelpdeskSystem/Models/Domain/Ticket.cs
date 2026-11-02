using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace HelpdeskSystem.Models.Domain
{
    public class Ticket
    {
        public int TicketId { get; set; }

        [Required]
        [StringLength(20)]
        public string TicketCode { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; }

        [Required]
        [AllowHtml]
        [Display(Name = "Mô tả chi tiết")]
        public string Description { get; set; }

        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }

        [Display(Name = "Mức ưu tiên")]
        public int PriorityId { get; set; }

        [Display(Name = "Trạng thái")]
        public TicketStatus Status { get; set; }

        public string CreatedById { get; set; }

        public string AssignedToId { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? AssignedDate { get; set; }

        public DateTime? FirstResponseDate { get; set; }

        public DateTime? ResolvedDate { get; set; }

        public DateTime? ClosedDate { get; set; }

        public DateTime? SLADueDate { get; set; }

        public bool IsSLAViolated { get; set; }

        public virtual Category Category { get; set; }
        public virtual Priority Priority { get; set; }
        public virtual ApplicationUser CreatedBy { get; set; }
        public virtual ApplicationUser AssignedTo { get; set; }
        public virtual TicketRating Rating { get; set; }

        public virtual ICollection<TicketComment> TicketComments { get; set; }
        public virtual ICollection<TicketAttachment> TicketAttachments { get; set; }
        public virtual ICollection<TicketHistory> TicketHistories { get; set; }

        public Ticket()
        {
            Status = TicketStatus.New;
            CreatedDate = DateTime.Now;
            IsSLAViolated = false;

            TicketComments = new HashSet<TicketComment>();
            TicketAttachments = new HashSet<TicketAttachment>();
            TicketHistories = new HashSet<TicketHistory>();
        }
    }
}
