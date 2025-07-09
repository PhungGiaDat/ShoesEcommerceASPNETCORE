using ShoesEcommerce.Models.Interactions;
using ShoesEcommerce.Models.Products;

namespace ShoesEcommerce.Models.Products
{
    public class Product
    {
        public int Id { get; set; } 

        public string Name { get; set; }

        
        public string Description { get; set; } 

        public int CategoryId { get; set; } 
        public Category Category { get; set; } 

        public int BrandId { get; set; } 

        public Brand Brand { get; set; }

        public ICollection<ProductVariant> Variants { get; set;  }
        public ICollection<Comment> Comments { get; set; } 
        public ICollection<QA> QAs { get; set; } 

        public ICollection<Favorite> Favorites { get; set;  }



    }
}
