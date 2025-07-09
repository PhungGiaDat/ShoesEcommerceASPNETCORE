using ShoesEcommerce.Models.Products;

namespace ShoesEcommerce.Models.Stocks
{
    public class StockTransaction
    {
        public int Id { get; set; }

        public int ProductVariantId { get; set; }
        public ProductVariant ProductVariant { get; set; }

        public string Type { get; set; } // "IN", "OUT", "ADJUST"
        public int Quantity { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Note { get; set; }
    }
}
