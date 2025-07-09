using ShoesEcommerce.Models.Carts;
using ShoesEcommerce.Models.Orders;
using ShoesEcommerce.Models.Stocks;

namespace ShoesEcommerce.Models.Products
{
    public class ProductVariant
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        public string Color { get; set;  }
        public string Size { get; set; } 

        public ICollection<Stock> Stocks { get; set; }
        public ICollection<CartItem> CartItems { get; set; }
        public ICollection<OrderDetail> OrderDetails { get; set; }
        public ICollection<StockEntry> StockEntries { get; set; }
        public ICollection<StockTransaction> StockTransactions { get; set; }

    }
}
