using ShoesEcommerce.Models.Stocks;


namespace ShoesEcommerce.Models.Products
{
    public class Supplier
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public ICollection<StockEntry> StockEntries { get; set; }
    }
}
