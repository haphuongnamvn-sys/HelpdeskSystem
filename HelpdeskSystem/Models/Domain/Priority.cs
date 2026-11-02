using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HelpdeskSystem.Models.Domain
{
    public class Priority
    {
        public int PriorityId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Mức ưu tiên")]
        public string Name { get; set; }

        public int Level { get; set; } // 1=Critical, 2=High, 3=Medium, 4=Low

        [StringLength(10)]
        public string ColorCode { get; set; } // e.g. "#dc3545"

        public virtual ICollection<Ticket> Tickets { get; set; }
        public virtual ICollection<SLAConfig> SLAConfigs { get; set; }

        public Priority()
        {
            Tickets = new HashSet<Ticket>();
            SLAConfigs = new HashSet<SLAConfig>();
        }
    }
}
