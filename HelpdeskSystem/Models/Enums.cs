using System;
using System.ComponentModel.DataAnnotations;

namespace HelpdeskSystem.Models
{
    public enum TicketStatus
    {
        [Display(Name = "Mới")]
        New = 0,
        [Display(Name = "Đã phân công")]
        Assigned = 1,
        [Display(Name = "Đang xử lý")]
        InProgress = 2,
        [Display(Name = "Tạm dừng")]
        Pending = 3,
        [Display(Name = "Đã giải quyết")]
        Resolved = 4,
        [Display(Name = "Đã đóng")]
        Closed = 5,
        [Display(Name = "Mở lại")]
        Reopened = 6
    }
}
