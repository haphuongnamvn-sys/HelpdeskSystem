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

        [Display(Name = "Chức vụ")]
        public string Position { get; set; }

        [Display(Name = "Địa chỉ")]
        public string Address { get; set; }

        [Display(Name = "Vai trò")]
        public string RoleName { get; set; }

        [Display(Name = "Vai trò")]
        public string SelectedRole { get; set; }

        [Display(Name = "Vai trò")]
        public string Role
        {
            get { return SelectedRole; }
            set { SelectedRole = value; }
        }

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

    public class UserCreateVM
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [StringLength(100)]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email / mã đăng nhập")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email đăng nhập")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu khởi tạo")]
        public string Password { get; set; }

        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Chức vụ")]
        public string Position { get; set; }

        [Display(Name = "Địa chỉ")]
        public string Address { get; set; }

        [Display(Name = "Phòng ban")]
        public int? DepartmentId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn vai trò")]
        [Display(Name = "Vai trò")]
        public string SelectedRole { get; set; }

        [Display(Name = "Vai trò")]
        public string Role
        {
            get { return SelectedRole; }
            set { SelectedRole = value; }
        }

        public IEnumerable<SelectListItem> Roles { get; set; }
        public IEnumerable<SelectListItem> Departments { get; set; }
    }
}
