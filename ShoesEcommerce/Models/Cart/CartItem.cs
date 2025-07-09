namespace ShoesEcommerce.Models.Cart
{
    public class CartItem
    {
        public int Id { get; set; } 

        public int CartId { get; set; }
        public Cart Cart { get; set; }

        public int ProductVarientId { get; set; }
        public ProductVariant ProductVariant { get; set; }

        public int Quantity { get; set; }

    }
}
