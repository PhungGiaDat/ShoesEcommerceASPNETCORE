using ShoesEcommerce.Models.Accounts;
using ShoesEcommerce.Models.Products;

namespace ShoesEcommerce.Models.Interactions
{
    public class Comment
    {
        public int Id { get; set; }

        public int CustomerId { get; set; }
        public Customer Customer { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        public string Content { get; set; }
        public int Rating { get; set; } // Số sao đánh giá (1-5)

        public DateTime CreatedAt { get; set; }
    }   
}
