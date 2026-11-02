using System;
using System.ComponentModel.DataAnnotations;

namespace HelpdeskSystem.Models.Domain
{
    public class TicketRating
    {
        [Key]
        public int RatingId { get; set; }

        public int TicketId { get; set; }

        public string UserId { get; set; }

        [Required]
        [Range(1, 5)]
        [Display(Name = "Đánh giá")]
        public int Stars { get; set; }

        [StringLength(1000)]
        [Display(Name = "Nhận xét")]
        public string Comment { get; set; }

        public DateTime RatedDate { get; set; }

        public virtual Ticket Ticket { get; set; }
        public virtual ApplicationUser User { get; set; }

        public TicketRating()
        {
            RatedDate = DateTime.Now;
        }
    }
}
