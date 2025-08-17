using ShoesEcommerce.Models.Accounts;

namespace ShoesEcommerce.Models.Carts
{
    public class Cart
    {
        public int Id { get; set; }
        public string SessionId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public ICollection<CartItem> CartItems { get; set; }
        
        // Navigation property để truy cập Customer thông qua Customer.CartId
        public Customer? Customer { get; set; }
    }
}
