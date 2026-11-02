using System.ComponentModel.DataAnnotations;

namespace HelpdeskSystem.Models.Domain
{
    public class SLAConfig
    {
        public int SLAConfigId { get; set; }

        [Display(Name = "Mức ưu tiên")]
        public int PriorityId { get; set; }

        [Display(Name = "Thời hạn phản hồi (giờ)")]
        [Range(1, 720)]
        public int ResponseTimeHours { get; set; }

        [Display(Name = "Thời hạn xử lý (giờ)")]
        [Range(1, 720)]
        public int ResolutionTimeHours { get; set; }

        [Display(Name = "Ngưỡng cảnh báo (%)")]
        [Range(50, 95)]
        public int WarningThresholdPercent { get; set; }

        public virtual Priority Priority { get; set; }

        public SLAConfig()
        {
            WarningThresholdPercent = 80;
        }
    }
}
