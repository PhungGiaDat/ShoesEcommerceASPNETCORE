using System.ComponentModel.DataAnnotations;

namespace ShoesEcommerce.ViewModels.Staff
{
    public class CreateStaffViewModel
    {
        [Required(ErrorMessage = "Firebase UID l� b?t bu?c")]
        [Display(Name = "Firebase UID")]
        public string FirebaseUid { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email l� b?t bu?c")]
        [EmailAddress(ErrorMessage = "Email kh�ng h?p l?")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "T�n l� b?t bu?c")]
        [StringLength(50, ErrorMessage = "T�n kh�ng ???c qu� 50 k� t?")]
        [Display(Name = "T�n")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "H? l� b?t bu?c")]
        [StringLength(50, ErrorMessage = "H? kh�ng ???c qu� 50 k� t?")]
        [Display(Name = "H?")]
        public string LastName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "S? ?i?n tho?i kh�ng h?p l?")]
        [Display(Name = "S? ?i?n tho?i")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ph�ng ban l� b?t bu?c")]
        [Display(Name = "Ph�ng ban")]
        public int DepartmentId { get; set; }
    }

    public class EditStaffViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Firebase UID l� b?t bu?c")]
        [Display(Name = "Firebase UID")]
        public string FirebaseUid { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email l� b?t bu?c")]
        [EmailAddress(ErrorMessage = "Email kh�ng h?p l?")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "T�n l� b?t bu?c")]
        [StringLength(50, ErrorMessage = "T�n kh�ng ???c qu� 50 k� t?")]
        [Display(Name = "T�n")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "H? l� b?t bu?c")]
        [StringLength(50, ErrorMessage = "H? kh�ng ???c qu� 50 k� t?")]
        [Display(Name = "H?")]
        public string LastName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "S? ?i?n tho?i kh�ng h?p l?")]
        [Display(Name = "S? ?i?n tho?i")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ph�ng ban l� b?t bu?c")]
        [Display(Name = "Ph�ng ban")]
        public int DepartmentId { get; set; }
    }

    public class StaffRoleViewModel
    {
        public int StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string StaffEmail { get; set; } = string.Empty;
        public List<RoleInfo> AssignedRoles { get; set; } = new();
        public List<RoleInfo> AvailableRoles { get; set; } = new();
    }

    public class RoleInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class StaffListViewModel
    {
        public IEnumerable<StaffInfo> Staffs { get; set; } = new List<StaffInfo>();
        public string SearchTerm { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }

    public class StaffInfo
    {
        public int Id { get; set; }
        public string FirebaseUid { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string PhoneNumber { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public List<string> RoleNames { get; set; } = new();
    }
}