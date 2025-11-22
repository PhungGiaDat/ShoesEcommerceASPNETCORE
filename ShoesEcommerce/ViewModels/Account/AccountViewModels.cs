using System.ComponentModel.DataAnnotations;
using ShoesEcommerce.Models.Accounts;
using StaffModel = ShoesEcommerce.Models.Accounts.Staff; // ? Resolve namespace conflict

namespace ShoesEcommerce.ViewModels.Account
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email là b?t bu?c")]
        [EmailAddress(ErrorMessage = "Email không h?p l?")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "M?t kh?u là b?t bu?c")]
        [DataType(DataType.Password)]
        [Display(Name = "M?t kh?u")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Ghi nh? ??ng nh?p")]
        public bool RememberMe { get; set; } = false;

        public string? ReturnUrl { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "H? là b?t bu?c")]
        [StringLength(50, ErrorMessage = "H? không ???c v??t quá 50 ký t?")]
        [Display(Name = "H?")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên là b?t bu?c")]
        [StringLength(50, ErrorMessage = "Tên không ???c v??t quá 50 ký t?")]
        [Display(Name = "Tên")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là b?t bu?c")]
        [EmailAddress(ErrorMessage = "Email không h?p l?")]
        [StringLength(255, ErrorMessage = "Email không ???c v??t quá 255 ký t?")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "S? ?i?n tho?i là b?t bu?c")]
        [RegularExpression(@"^(0[3|5|7|8|9])[0-9]{8}$", ErrorMessage = "S? ?i?n tho?i ph?i có 10 ch? s? và b?t ??u b?ng 03, 05, 07, 08, ho?c 09")]
        [Display(Name = "S? ?i?n tho?i")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "M?t kh?u là b?t bu?c")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "M?t kh?u ph?i có t? 6 ??n 100 ký t?")]
        [DataType(DataType.Password)]
        [Display(Name = "M?t kh?u")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Xác nh?n m?t kh?u là b?t bu?c")]
        [DataType(DataType.Password)]
        [Display(Name = "Xác nh?n m?t kh?u")]
        [Compare("Password", ErrorMessage = "M?t kh?u và xác nh?n m?t kh?u không kh?p")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngày sinh là b?t bu?c")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateTime DateOfBirth { get; set; } = new DateTime(1990, 1, 1); // Set reasonable default instead of MinValue

        [Display(Name = "Tôi ??ng ý v?i các ?i?u kho?n và ?i?u ki?n")]
        [Range(typeof(bool), "true", "true", ErrorMessage = "B?n ph?i ??ng ý v?i các ?i?u kho?n và ?i?u ki?n")]
        public bool AcceptTerms { get; set; } = false;

        // Custom validation method for date of birth
        public static ValidationResult ValidateDateOfBirth(DateTime dateOfBirth, ValidationContext context)
        {
            var today = DateTime.Today;
            var age = today.Year - dateOfBirth.Year;
            
            // Check if birthday has occurred this year
            if (dateOfBirth.Date > today.AddYears(-age))
                age--;

            if (dateOfBirth > today)
            {
                return new ValidationResult("Ngày sinh không th? là ngày trong t??ng lai");
            }

            if (age < 13)
            {
                return new ValidationResult("B?n ph?i ?? 13 tu?i ?? ??ng ký tài kho?n");
            }

            if (age > 150)
            {
                return new ValidationResult("Ngày sinh không h?p l?");
            }

            return ValidationResult.Success;
        }
    }

    public class CustomerProfileViewModel
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string? ImageUrl { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public string FullName => $"{FirstName} {LastName}".Trim();
    }

    public class UpdateProfileViewModel
    {
        [Required(ErrorMessage = "H? là b?t bu?c")]
        [StringLength(50, ErrorMessage = "H? không ???c v??t quá 50 ký t?")]
        [Display(Name = "H?")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên là b?t bu?c")]
        [StringLength(50, ErrorMessage = "Tên không ???c v??t quá 50 ký t?")]
        [Display(Name = "Tên")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "S? ?i?n tho?i là b?t bu?c")]
        [RegularExpression(@"^\+?\d{10,15}$", ErrorMessage = "S? ?i?n tho?i ph?i có 10-15 ch? s?")]
        [Display(Name = "S? ?i?n tho?i")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngày sinh là b?t bu?c")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateTime DateOfBirth { get; set; } = new DateTime(1990, 1, 1); // Set reasonable default instead of MinValue

        [Display(Name = "Hình ??i di?n")]
        public IFormFile? ProfileImage { get; set; }

        public string? CurrentImageUrl { get; set; }
    }

    public class UpdateAddressViewModel
    {
        [StringLength(100, ErrorMessage = "??a ch? không ???c v??t quá 100 ký t?")]
        [Display(Name = "??a ch?")]
        public string? Address { get; set; }

        [StringLength(50, ErrorMessage = "Thành ph? không ???c v??t quá 50 ký t?")]
        [Display(Name = "Thành ph?")]
        public string? City { get; set; }

        [StringLength(50, ErrorMessage = "T?nh/Thành ph? không ???c v??t quá 50 ký t?")]
        [Display(Name = "T?nh/Thành ph?")]
        public string? State { get; set; }
    }

    public class CustomerListViewModel
    {
        public IEnumerable<CustomerInfo> Customers { get; set; } = new List<CustomerInfo>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public string SearchTerm { get; set; } = string.Empty;
        public bool HasPrevious => PageNumber > 1;
        public bool HasNext => PageNumber < TotalPages;
    }

    public class CustomerInfo
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int TotalOrders { get; set; }
        public bool IsActive { get; set; } = true;
        public string FullName => $"{FirstName} {LastName}".Trim();
    }

    public class CustomerRegistrationResult
    {
        public bool Success { get; set; }
        public Customer? Customer { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public Dictionary<string, string> ValidationErrors { get; set; } = new();
    }

    public class CustomerLoginResult
    {
        public bool Success { get; set; }
        public Customer? Customer { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public bool RequiresTwoFactor { get; set; } = false;
        public bool IsLockedOut { get; set; } = false;
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "M?t kh?u hi?n t?i là b?t bu?c")]
        [DataType(DataType.Password)]
        [Display(Name = "M?t kh?u hi?n t?i")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "M?t kh?u m?i là b?t bu?c")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "M?t kh?u ph?i có ít nh?t 6 ký t?")]
        [DataType(DataType.Password)]
        [Display(Name = "M?t kh?u m?i")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Xác nh?n m?t kh?u m?i là b?t bu?c")]
        [DataType(DataType.Password)]
        [Display(Name = "Xác nh?n m?t kh?u m?i")]
        [Compare("NewPassword", ErrorMessage = "M?t kh?u m?i và xác nh?n m?t kh?u không kh?p")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    // ===== STAFF REGISTRATION (NEW - Following SOLID & Clean Architecture) =====

    /// <summary>
    /// ViewModel for staff registration
    /// Used by Admin to create new staff accounts
    /// </summary>
    public class RegisterStaffViewModel
    {
        [Required(ErrorMessage = "H? là b?t bu?c")]
        [StringLength(50, ErrorMessage = "H? không ???c v??t quá 50 ký t?")]
        [Display(Name = "H?")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên là b?t bu?c")]
        [StringLength(50, ErrorMessage = "Tên không ???c v??t quá 50 ký t?")]
        [Display(Name = "Tên")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là b?t bu?c")]
        [EmailAddress(ErrorMessage = "Email không h?p l?")]
        [StringLength(255, ErrorMessage = "Email không ???c v??t quá 255 ký t?")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "S? ?i?n tho?i là b?t bu?c")]
        [RegularExpression(@"^(0[3|5|7|8|9])[0-9]{8}$", 
            ErrorMessage = "S? ?i?n tho?i ph?i có 10 ch? s? và b?t ??u b?ng 03, 05, 07, 08, ho?c 09")]
        [Display(Name = "S? ?i?n tho?i")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "M?t kh?u là b?t bu?c")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "M?t kh?u ph?i có t? 6 ??n 100 ký t?")]
        [DataType(DataType.Password)]
        [Display(Name = "M?t kh?u")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Xác nh?n m?t kh?u là b?t bu?c")]
        [DataType(DataType.Password)]
        [Display(Name = "Xác nh?n m?t kh?u")]
        [Compare("Password", ErrorMessage = "M?t kh?u và xác nh?n m?t kh?u không kh?p")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phòng ban là b?t bu?c")]
        [Display(Name = "Phòng ban")]
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng ch?n phòng ban")]
        public int DepartmentId { get; set; } = 0;

        [Required(ErrorMessage = "Vai trò là b?t bu?c")]
        [Display(Name = "Vai trò")]
        [StringLength(50, ErrorMessage = "Vai trò không ???c v??t quá 50 ký t?")]
        public string RoleName { get; set; } = "Staff"; // Default to Staff role

        // Helper property for display
        public string FullName => $"{FirstName} {LastName}".Trim();

        // Available roles (populated by controller)
        public List<string> AvailableRoles { get; set; } = new() { "Admin", "Manager", "Staff" };
    }

    /// <summary>
    /// Result of staff registration operation
    /// Contains success status, created staff, and error information
    /// </summary>
    public class StaffRegistrationResult
    {
        public bool Success { get; set; }
        public StaffModel? Staff { get; set; } // ? Use alias
        public string ErrorMessage { get; set; } = string.Empty;
        public Dictionary<string, string> ValidationErrors { get; set; } = new();

        // Helper method to add validation error
        public void AddValidationError(string field, string message)
        {
            ValidationErrors[field] = message;
        }

        // Helper property to check if has validation errors
        public bool HasValidationErrors => ValidationErrors.Any();
    }

    /// <summary>
    /// Staff login result (similar to CustomerLoginResult)
    /// </summary>
    public class StaffLoginResult
    {
        public bool Success { get; set; }
        public StaffModel? Staff { get; set; } // ? Use alias
        public string ErrorMessage { get; set; } = string.Empty;
        public bool RequiresTwoFactor { get; set; } = false;
        public bool IsLockedOut { get; set; } = false;
    }
}