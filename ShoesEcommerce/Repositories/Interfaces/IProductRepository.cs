using ShoesEcommerce.Models.Products;

namespace ShoesEcommerce.Repositories.Interfaces
{
    public interface IProductRepository
    {
        // Product CRUD
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<Product?> GetProductByIdAsync(int id);
        Task<Product> CreateProductAsync(Product product);
        Task<Product> UpdateProductAsync(Product product);
        Task<bool> DeleteProductAsync(int id);
        Task<bool> ProductExistsAsync(int id);

        // Product Variants
        Task<IEnumerable<ProductVariant>> GetProductVariantsAsync(int productId);
        Task<ProductVariant?> GetProductVariantByIdAsync(int id);
        Task<ProductVariant> CreateProductVariantAsync(ProductVariant variant);
        Task<ProductVariant> UpdateProductVariantAsync(ProductVariant variant);
        Task<bool> DeleteProductVariantAsync(int id);

        // Categories
        Task<IEnumerable<Category>> GetAllCategoriesAsync();
        Task<Category?> GetCategoryByIdAsync(int id);
        Task<Category> CreateCategoryAsync(Category category);
        Task<Category> UpdateCategoryAsync(Category category);
        Task<bool> DeleteCategoryAsync(int id);

        // Brands
        Task<IEnumerable<Brand>> GetAllBrandsAsync();
        Task<Brand?> GetBrandByIdAsync(int id);
        Task<Brand> CreateBrandAsync(Brand brand);
        Task<Brand> UpdateBrandAsync(Brand brand);
        Task<bool> DeleteBrandAsync(int id);

        // Suppliers
        Task<IEnumerable<Supplier>> GetAllSuppliersAsync();
        Task<Supplier?> GetSupplierByIdAsync(int id);

        // Advanced Queries
        Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm);
        Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId);
        Task<IEnumerable<Product>> GetProductsByBrandAsync(int brandId);
        Task<IEnumerable<Product>> GetPaginatedProductsAsync(int pageNumber, int pageSize, string searchTerm = "", int? categoryId = null, int? brandId = null);
        Task<int> GetTotalProductCountAsync(string searchTerm = "", int? categoryId = null, int? brandId = null);
    }
}