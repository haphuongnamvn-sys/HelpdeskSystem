using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HelpdeskSystem.Models.Domain
{
    public class Category
    {
        public int CategoryId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Tên danh mục")]
        public string Name { get; set; }

        [StringLength(500)]
        [Display(Name = "Mô tả")]
        public string Description { get; set; }

        public int? DepartmentId { get; set; }

        [Display(Name = "Đang hoạt động")]
        public bool IsActive { get; set; }

        public virtual Department Department { get; set; }
        public virtual ICollection<Ticket> Tickets { get; set; }

        public Category()
        {
            IsActive = true;
            Tickets = new HashSet<Ticket>();
        }
    }
}
