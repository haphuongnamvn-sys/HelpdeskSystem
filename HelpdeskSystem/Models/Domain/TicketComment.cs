using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace HelpdeskSystem.Models.Domain
{
    public class TicketComment
    {
        [Key]
        public int CommentId { get; set; }

        public int TicketId { get; set; }

        public string UserId { get; set; }

        [Required]
        [AllowHtml]
        [Display(Name = "Nội dung")]
        public string Content { get; set; }

        [Display(Name = "Ghi chú nội bộ")]
        public bool IsInternalNote { get; set; }

        public DateTime CreatedDate { get; set; }

        public virtual Ticket Ticket { get; set; }
        public virtual ApplicationUser User { get; set; }

        public virtual ICollection<TicketAttachment> Attachments { get; set; }

        public TicketComment()
        {
            IsInternalNote = false;
            CreatedDate = DateTime.Now;
            Attachments = new HashSet<TicketAttachment>();
        }
    }
}
