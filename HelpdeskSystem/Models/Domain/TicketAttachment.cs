using System;
using System.ComponentModel.DataAnnotations;

namespace HelpdeskSystem.Models.Domain
{
    public class TicketAttachment
    {
        [Key]
        public int AttachmentId { get; set; }

        public int TicketId { get; set; }

        public int? CommentId { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; }

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; }

        public long FileSize { get; set; }

        public DateTime UploadedDate { get; set; }

        public virtual Ticket Ticket { get; set; }
        public virtual TicketComment Comment { get; set; }

        public TicketAttachment()
        {
            UploadedDate = DateTime.Now;
        }
    }
}
