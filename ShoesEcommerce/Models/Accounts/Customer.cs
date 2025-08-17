using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using ShoesEcommerce.Models.Interactions;
using ShoesEcommerce.Models.Orders;
using ShoesEcommerce.Models.Products;
using ShoesEcommerce.Models.Carts;

namespace ShoesEcommerce.Models.Accounts
{
    public class Customer
    {
        
        public int Id { get; set; }

        [Required]
        public string FirebaseUid { get; set; }

        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; }

        public string Email { get; set; }

        [Required]
        [RegularExpression(@"^\+?\d{10,15}$", ErrorMessage = "Phone number must be 10-15 digits and may start with +")]
        public string PhoneNumber { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Url]
        public string ImageUrl { get; set; }

        [MaxLength(100)]    
        public string Address { get; set; }
        [MaxLength(50)]
        public string City { get; set; }
        [MaxLength(50)]
        public string State { get; set; }
        

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Add CartId to match database schema
        public int? CartId { get; set; }
        public Cart Cart { get; set; }
        public ICollection<Order> Orders { get; set; }
        public ICollection<ShippingAddress> ShippingAddresses { get; set; }
        public ICollection<Comment> Comments { get; set; }
        public ICollection<QA> QAs { get; set; }
        public ICollection<Favorite> Favorites { get; set; }

        public ICollection<UserRole> Roles { get; set; }
    }
}
