using ShoesEcommerce.Models.Products;

namespace ShoesEcommerce.Models.Stocks
{
    public class Stock
    {
        public int Id { get; set; }

        public int ProductVariantId { get; set; }
        public ProductVariant ProductVariant { get; set; }

        public int Quantity { get; set; }
    }
}
