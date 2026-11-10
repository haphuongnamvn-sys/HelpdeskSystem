using System;
using System.ComponentModel.DataAnnotations;

namespace HelpdeskSystem.Models.Domain
{
    public class ArticleRating
    {
        [Key]
        public int ArticleRatingId { get; set; }

        public int ArticleId { get; set; }

        public string UserId { get; set; }

        public bool IsHelpful { get; set; }

        public DateTime CreatedDate { get; set; }

        public virtual KnowledgeBase Article { get; set; }
        public virtual ApplicationUser User { get; set; }

        public ArticleRating()
        {
            CreatedDate = DateTime.Now;
        }
    }
}
