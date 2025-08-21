using System.ComponentModel.DataAnnotations;
using ShoesEcommerce.Models.Accounts;

namespace ShoesEcommerce.ViewModels.Account
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email l� b?t bu?c")]
        [EmailAddress(ErrorMessage = "Email kh�ng h?p l?")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "M?t kh?u l� b?t bu?c")]
        [DataType(DataType.Password)]
        [Display(Name = "M?t kh?u")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Ghi nh? ??ng nh?p")]
        public bool RememberMe { get; set; } = false;

        public string? ReturnUrl { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "H? l� b?t bu?c")]
        [StringLength(50, ErrorMessage = "H? kh�ng ???c v??t qu� 50 k� t?")]
        [Display(Name = "H?")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "T�n l� b?t bu?c")]
        [StringLength(50, ErrorMessage = "T�n kh�ng ???c v??t qu� 50 k� t?")]
        [Display(Name = "T�n")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email l� b?t bu?c")]
        [EmailAddress(ErrorMessage = "Email kh�ng h?p l?")]
        [StringLength(255, ErrorMessage = "Email kh�ng ???c v??t qu� 255 k� t?")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "S? ?i?n tho?i l� b?t bu?c")]
        [RegularExpression(@"^(0[3|5|7|8|9])[0-9]{8}$", ErrorMessage = "S? ?i?n tho?i ph?i c� 10 ch? s? v� b?t ??u b?ng 03, 05, 07, 08, ho?c 09")]
        [Display(Name = "S? ?i?n tho?i")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "M?t kh?u l� b?t bu?c")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "M?t kh?u ph?i c� t? 6 ??n 100 k� t?")]
        [DataType(DataType.Password)]
        [Display(Name = "M?t kh?u")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "X�c nh?n m?t kh?u l� b?t bu?c")]
        [DataType(DataType.Password)]
        [Display(Name = "X�c nh?n m?t kh?u")]
        [Compare("Password", ErrorMessage = "M?t kh?u v� x�c nh?n m?t kh?u kh�ng kh?p")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ng�y sinh l� b?t bu?c")]
        [DataType(DataType.Date)]
        [Display(Name = "Ng�y sinh")]
        public DateTime DateOfBirth { get; set; } = new DateTime(1990, 1, 1); // Set reasonable default instead of MinValue

        [Display(Name = "T�i ??ng � v?i c�c ?i?u kho?n v� ?i?u ki?n")]
        [Range(typeof(bool), "true", "true", ErrorMessage = "B?n ph?i ??ng � v?i c�c ?i?u kho?n v� ?i?u ki?n")]
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
                return new ValidationResult("Ng�y sinh kh�ng th? l� ng�y trong t??ng lai");
            }

            if (age < 13)
            {
                return new ValidationResult("B?n ph?i ?? 13 tu?i ?? ??ng k� t�i kho?n");
            }

            if (age > 150)
            {
                return new ValidationResult("Ng�y sinh kh�ng h?p l?");
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
        [Required(ErrorMessage = "H? l� b?t bu?c")]
        [StringLength(50, ErrorMessage = "H? kh�ng ???c v??t qu� 50 k� t?")]
        [Display(Name = "H?")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "T�n l� b?t bu?c")]
        [StringLength(50, ErrorMessage = "T�n kh�ng ???c v??t qu� 50 k� t?")]
        [Display(Name = "T�n")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "S? ?i?n tho?i l� b?t bu?c")]
        [RegularExpression(@"^\+?\d{10,15}$", ErrorMessage = "S? ?i?n tho?i ph?i c� 10-15 ch? s?")]
        [Display(Name = "S? ?i?n tho?i")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ng�y sinh l� b?t bu?c")]
        [DataType(DataType.Date)]
        [Display(Name = "Ng�y sinh")]
        public DateTime DateOfBirth { get; set; } = new DateTime(1990, 1, 1); // Set reasonable default instead of MinValue

        [Display(Name = "H�nh ??i di?n")]
        public IFormFile? ProfileImage { get; set; }

        public string? CurrentImageUrl { get; set; }
    }

    public class UpdateAddressViewModel
    {
        [StringLength(100, ErrorMessage = "??a ch? kh�ng ???c v??t qu� 100 k� t?")]
        [Display(Name = "??a ch?")]
        public string? Address { get; set; }

        [StringLength(50, ErrorMessage = "Th�nh ph? kh�ng ???c v??t qu� 50 k� t?")]
        [Display(Name = "Th�nh ph?")]
        public string? City { get; set; }

        [StringLength(50, ErrorMessage = "T?nh/Th�nh ph? kh�ng ???c v??t qu� 50 k� t?")]
        [Display(Name = "T?nh/Th�nh ph?")]
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
        [Required(ErrorMessage = "M?t kh?u hi?n t?i l� b?t bu?c")]
        [DataType(DataType.Password)]
        [Display(Name = "M?t kh?u hi?n t?i")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "M?t kh?u m?i l� b?t bu?c")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "M?t kh?u ph?i c� �t nh?t 6 k� t?")]
        [DataType(DataType.Password)]
        [Display(Name = "M?t kh?u m?i")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "X�c nh?n m?t kh?u m?i l� b?t bu?c")]
        [DataType(DataType.Password)]
        [Display(Name = "X�c nh?n m?t kh?u m?i")]
        [Compare("NewPassword", ErrorMessage = "M?t kh?u m?i v� x�c nh?n m?t kh?u kh�ng kh?p")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}