using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace HelpdeskSystem.Models.ViewModels
{
    public class UserManagementVM
    {
        public string Id { get; set; }

        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Vai trò")]
        public string RoleName { get; set; }

        [Display(Name = "Vai trò")]
        public string SelectedRole { get; set; }

        [Display(Name = "Phòng ban")]
        public int? DepartmentId { get; set; }

        [Display(Name = "Phòng ban")]
        public string DepartmentName { get; set; }

        [Display(Name = "Trạng thái")]
        public bool IsActive { get; set; }

        [Display(Name = "Ngày tạo")]
        public string CreatedDate { get; set; }

        public IEnumerable<SelectListItem> Roles { get; set; }
        public IEnumerable<SelectListItem> Departments { get; set; }
    }
}
