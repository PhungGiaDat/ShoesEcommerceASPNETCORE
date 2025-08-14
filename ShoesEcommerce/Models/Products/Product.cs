using ShoesEcommerce.Models.Interactions;
using ShoesEcommerce.Models.Products;

namespace ShoesEcommerce.Models.Products
{
    public class Product
    {
        public int Id { get; set; } 

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int CategoryId { get; set; } 
        public Category Category { get; set; } 

        public int BrandId { get; set; } 

        public Brand Brand { get; set; }

        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<QA> QAs { get; set; } = new List<QA>();

        public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    }
}
