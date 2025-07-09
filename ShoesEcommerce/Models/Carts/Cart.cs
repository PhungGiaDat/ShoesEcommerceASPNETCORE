namespace ShoesEcommerce.Models.Carts
{
    public class Cart
    {
        public int Id { get; set; }
        public string UserId { get; set; }

        public string SessionId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public ICollection<CartItem> CartItems { get; set; }

    }
}
