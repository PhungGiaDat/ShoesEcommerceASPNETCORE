using ShoesEcommerce.Models.Products;

namespace ShoesEcommerce.Models.Stocks
{
    public class StockEntry
    {
        public int Id { get; set; }

        public int ProductVariantId { get; set; }
        public ProductVariant ProductVariant { get; set; }

        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }

        public int Quantity { get; set; }
        public DateTime EntryDate { get; set; }
    }
}
