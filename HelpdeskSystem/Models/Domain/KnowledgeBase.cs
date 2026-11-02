using System;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace HelpdeskSystem.Models.Domain
{
    public class KnowledgeBase
    {
        [Key]
        public int ArticleId { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; }

        [Required]
        [AllowHtml]
        [Display(Name = "Nội dung")]
        public string Content { get; set; }

        public int CategoryId { get; set; }

        public string CreatedById { get; set; }

        public int ViewCount { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public virtual Category Category { get; set; }
        public virtual ApplicationUser CreatedBy { get; set; }

        public KnowledgeBase()
        {
            ViewCount = 0;
            CreatedDate = DateTime.Now;
        }
    }
}
