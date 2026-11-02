using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;
using System.Web.Mvc;
using HelpdeskSystem.Models.Domain;

namespace HelpdeskSystem.Models.ViewModels
{
    // Used for creating a new ticket
    public class TicketCreateVM
    {
        [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
        [StringLength(200)]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mô tả")]
        [AllowHtml]
        [Display(Name = "Mô tả chi tiết")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn mức ưu tiên")]
        [Display(Name = "Mức ưu tiên")]
        public int PriorityId { get; set; }

        [Display(Name = "Tệp đính kèm")]
        public HttpPostedFileBase[] Attachments { get; set; }

        // Dropdowns
        public IEnumerable<SelectListItem> Categories { get; set; }
        public IEnumerable<SelectListItem> Priorities { get; set; }
    }

    // Used for ticket detail page
    public class TicketDetailVM
    {
        public int TicketId { get; set; }
        public string TicketCode { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string CategoryName { get; set; }
        public string PriorityName { get; set; }
        public string PriorityColor { get; set; }
        public TicketStatus Status { get; set; }
        public string StatusDisplay { get; set; }
        public string StatusColor { get; set; }
        public string CreatedByName { get; set; }
        public string CreatedById { get; set; }
        public string AssignedToName { get; set; }
        public string AssignedToId { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? AssignedDate { get; set; }
        public DateTime? ResolvedDate { get; set; }
        public DateTime? ClosedDate { get; set; }
        public DateTime? SLADueDate { get; set; }
        public bool IsSLAViolated { get; set; }
        public string SLAStatusText { get; set; }
        public string SLAStatusColor { get; set; }

        // For assignment dropdown (Admin only)
        public IEnumerable<SelectListItem> Technicians { get; set; }
        public string SelectedTechnicianId { get; set; }

        // Comments
        public List<CommentItem> Comments { get; set; }
        public string NewComment { get; set; }
        public bool IsInternalNote { get; set; }

        // History
        public List<HistoryItem> History { get; set; }

        // Attachments
        public List<AttachmentItem> Attachments { get; set; }

        // Rating
        public int? RatingStars { get; set; }
        public string RatingComment { get; set; }
        public bool CanRate { get; set; } // true if user is creator and status is Resolved/Closed and not yet rated
        public bool HasRated { get; set; }

        // Permissions
        public bool CanAssign { get; set; }
        public bool CanChangeStatus { get; set; }
        public bool CanComment { get; set; }
        public bool CanAddInternalNote { get; set; }
        public List<TicketStatus> AllowedTransitions { get; set; }

        public TicketDetailVM()
        {
            Comments = new List<CommentItem>();
            History = new List<HistoryItem>();
            Attachments = new List<AttachmentItem>();
            AllowedTransitions = new List<TicketStatus>();
        }
    }

    public class CommentItem
    {
        public int CommentId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string UserRole { get; set; }
        public string Content { get; set; }
        public bool IsInternalNote { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<AttachmentItem> Attachments { get; set; }

        public CommentItem()
        {
            Attachments = new List<AttachmentItem>();
        }
    }

    public class HistoryItem
    {
        public string ChangedByName { get; set; }
        public string FieldChanged { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public DateTime ChangedDate { get; set; }
    }

    public class AttachmentItem
    {
        public int AttachmentId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string FileSize { get; set; }
        public DateTime UploadedDate { get; set; }
    }

    // Used for ticket list page
    public class TicketListVM
    {
        public List<TicketListItem> Tickets { get; set; }
        public TicketFilterVM Filter { get; set; }

        public TicketListVM()
        {
            Tickets = new List<TicketListItem>();
            Filter = new TicketFilterVM();
        }
    }

    public class TicketListItem
    {
        public int TicketId { get; set; }
        public string TicketCode { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string CategoryName { get; set; }
        public string PriorityName { get; set; }
        public string PriorityColor { get; set; }
        public string StatusDisplay { get; set; }
        public string StatusColor { get; set; }
        public string CreatedByName { get; set; }
        public string AssignedToName { get; set; }
        public string CreatedDate { get; set; }
        public string SLAStatus { get; set; }
        public string SLAStatusColor { get; set; }
    }

    public class TicketFilterVM
    {
        [Display(Name = "Trạng thái")]
        public TicketStatus? Status { get; set; }

        [Display(Name = "Danh mục")]
        public int? CategoryId { get; set; }

        [Display(Name = "Mức ưu tiên")]
        public int? PriorityId { get; set; }

        [Display(Name = "Từ ngày")]
        [DataType(DataType.Date)]
        public DateTime? FromDate { get; set; }

        [Display(Name = "Đến ngày")]
        [DataType(DataType.Date)]
        public DateTime? ToDate { get; set; }

        [Display(Name = "Tìm kiếm")]
        public string SearchTerm { get; set; }

        public IEnumerable<SelectListItem> Categories { get; set; }
        public IEnumerable<SelectListItem> Priorities { get; set; }
        public IEnumerable<SelectListItem> Statuses { get; set; }
    }

    // Used for editing ticket (Admin)
    public class TicketEditVM
    {
        public int TicketId { get; set; }
        public string TicketCode { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; }

        [Required]
        [AllowHtml]
        [Display(Name = "Mô tả")]
        public string Description { get; set; }

        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }

        [Display(Name = "Mức ưu tiên")]
        public int PriorityId { get; set; }

        public IEnumerable<SelectListItem> Categories { get; set; }
        public IEnumerable<SelectListItem> Priorities { get; set; }
    }
}
