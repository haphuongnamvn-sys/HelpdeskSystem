using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HelpdeskSystem.Models.Domain
{
    public class Department
    {
        public int DepartmentId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Tên phòng ban")]
        public string Name { get; set; }

        [StringLength(500)]
        [Display(Name = "Mô tả")]
        public string Description { get; set; }

        public bool IsActive { get; set; }

        public virtual ICollection<Category> Categories { get; set; }
        public virtual ICollection<ApplicationUser> Users { get; set; }

        public Department()
        {
            IsActive = true;
            Categories = new HashSet<Category>();
            Users = new HashSet<ApplicationUser>();
        }
    }
}
