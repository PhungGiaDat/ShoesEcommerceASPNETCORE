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
        public string FisebaseUid { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string ImageUrl { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;



        public Cart Cart { get; set; }
        public ICollection<Order> Orders { get; set; }
        public ICollection<ShippingAddress> ShippingAddresses { get; set; }
        public ICollection<Comment> Comments { get; set; }
        public ICollection<QA> QAs { get; set; }
        public ICollection<Favorite> Favorites { get; set; }
    }
}
