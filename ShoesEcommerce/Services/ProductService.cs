using ShoesEcommerce.Models.Products;
using ShoesEcommerce.Repositories.Interfaces;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.ViewModels.Product;

namespace ShoesEcommerce.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly ILogger<ProductService> _logger;

        public ProductService(IProductRepository productRepository, ILogger<ProductService> logger)
        {
            _productRepository = productRepository;
            _logger = logger;
        }

        // Product Management
        public async Task<ProductListViewModel> GetProductsAsync(string searchTerm, int? categoryId, int? brandId, int page, int pageSize)
        {
            try
            {
                var products = await _productRepository.GetPaginatedProductsAsync(page, pageSize, searchTerm, categoryId, brandId);
                var totalCount = await _productRepository.GetTotalProductCountAsync(searchTerm, categoryId, brandId);
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                // Map to ProductInfo
                var productInfos = products.Select(p => new ProductInfo
                {
                    Id = p.Id,
                    Name = p.Name ?? string.Empty,
                    Description = p.Description ?? string.Empty,
                    Price = p.Price,
                    CategoryName = p.Category?.Name ?? "N/A",
                    BrandName = p.Brand?.Name ?? "N/A",
                    VariantCount = p.Variants?.Count ?? 0,
                    TotalStock = 0, // Will be calculated from Stock table later
                    CreatedDate = DateTime.Now, // Add this field to your model if needed
                    IsActive = true // Add this field to your model if needed
                }).ToList();

                return new ProductListViewModel
                {
                    Products = productInfos,
                    SearchTerm = searchTerm ?? string.Empty,
                    CategoryId = categoryId,
                    BrandId = brandId,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    PageSize = pageSize,
                    TotalCount = totalCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting products");
                return new ProductListViewModel
                {
                    Products = new List<ProductInfo>(),
                    SearchTerm = searchTerm ?? string.Empty,
                    CategoryId = categoryId,
                    BrandId = brandId,
                    CurrentPage = page,
                    TotalPages = 0,
                    PageSize = pageSize,
                    TotalCount = 0
                };
            }
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            try
            {
                return await _productRepository.GetProductByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting product by ID: {ProductId}", id);
                return null;
            }
        }

        public async Task<ProductInfo> CreateProductAsync(CreateProductViewModel model)
        {
            try
            {
                if (!await ValidateProductDataAsync(model))
                    throw new InvalidOperationException("Product data validation failed");

                var product = new Product
                {
                    Name = model.Name,
                    Description = model.Description,
                    Price = model.Price,
                    CategoryId = model.CategoryId,
                    BrandId = model.BrandId
                };

                var createdProduct = await _productRepository.CreateProductAsync(product);

                // Get related data for return value
                var category = await _productRepository.GetCategoryByIdAsync(model.CategoryId);
                var brand = await _productRepository.GetBrandByIdAsync(model.BrandId);

                return new ProductInfo
                {
                    Id = createdProduct.Id,
                    Name = createdProduct.Name,
                    Description = createdProduct.Description,
                    Price = createdProduct.Price,
                    CategoryName = category?.Name ?? "N/A",
                    BrandName = brand?.Name ?? "N/A",
                    VariantCount = 0,
                    TotalStock = 0,
                    CreatedDate = DateTime.Now,
                    IsActive = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating product");
                throw new InvalidOperationException("Unable to create product", ex);
            }
        }

        public async Task<bool> UpdateProductAsync(int id, EditProductViewModel model)
        {
            try
            {
                if (!await ValidateProductUpdateAsync(id, model))
                    return false;

                var existingProduct = await _productRepository.GetProductByIdAsync(id);
                if (existingProduct == null)
                    return false;

                existingProduct.Name = model.Name;
                existingProduct.Description = model.Description;
                existingProduct.Price = model.Price;
                existingProduct.CategoryId = model.CategoryId;
                existingProduct.BrandId = model.BrandId;

                await _productRepository.UpdateProductAsync(existingProduct);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating product with ID: {ProductId}", id);
                return false;
            }
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            try
            {
                if (!await CanDeleteProductAsync(id))
                    return false;

                return await _productRepository.DeleteProductAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting product with ID: {ProductId}", id);
                return false;
            }
        }

        public async Task<bool> ProductExistsAsync(int id)
        {
            try
            {
                return await _productRepository.ProductExistsAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking if product exists: {ProductId}", id);
                return false;
            }
        }

        // Product Variants
        public async Task<IEnumerable<ProductVariantInfo>> GetProductVariantsAsync(int productId)
        {
            try
            {
                var variants = await _productRepository.GetProductVariantsAsync(productId);
                return variants.Select(v => new ProductVariantInfo
                {
                    Id = v.Id,
                    ProductId = v.ProductId,
                    ProductName = v.Product?.Name ?? "N/A",
                    Color = v.Color ?? string.Empty,
                    Size = v.Size ?? string.Empty,
                    StockQuantity = 0, // Will be calculated from Stock table later
                    Price = v.Product?.Price ?? 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting product variants for product: {ProductId}", productId);
                return new List<ProductVariantInfo>();
            }
        }

        public async Task<ProductVariant?> GetProductVariantByIdAsync(int id)
        {
            try
            {
                return await _productRepository.GetProductVariantByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting product variant by ID: {VariantId}", id);
                return null;
            }
        }

        public async Task<ProductVariantInfo> CreateProductVariantAsync(CreateProductVariantViewModel model)
        {
            try
            {
                var variant = new ProductVariant
                {
                    ProductId = model.ProductId,
                    Color = model.Color,
                    Size = model.Size
                };

                var createdVariant = await _productRepository.CreateProductVariantAsync(variant);
                var product = await _productRepository.GetProductByIdAsync(model.ProductId);

                return new ProductVariantInfo
                {
                    Id = createdVariant.Id,
                    ProductId = createdVariant.ProductId,
                    ProductName = product?.Name ?? "N/A",
                    Color = createdVariant.Color,
                    Size = createdVariant.Size,
                    StockQuantity = 0,
                    Price = product?.Price ?? 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating product variant");
                throw new InvalidOperationException("Unable to create product variant", ex);
            }
        }

        public async Task<bool> UpdateProductVariantAsync(int id, EditProductVariantViewModel model)
        {
            try
            {
                var existingVariant = await _productRepository.GetProductVariantByIdAsync(id);
                if (existingVariant == null)
                    return false;

                existingVariant.Color = model.Color;
                existingVariant.Size = model.Size;

                await _productRepository.UpdateProductVariantAsync(existingVariant);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating product variant with ID: {VariantId}", id);
                return false;
            }
        }

        public async Task<bool> DeleteProductVariantAsync(int id)
        {
            try
            {
                return await _productRepository.DeleteProductVariantAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting product variant with ID: {VariantId}", id);
                return false;
            }
        }

        // Categories
        public async Task<IEnumerable<CategoryInfo>> GetAllCategoriesAsync()
        {
            try
            {
                var categories = await _productRepository.GetAllCategoriesAsync();
                return categories.Select(c => new CategoryInfo
                {
                    Id = c.Id,
                    Name = c.Name ?? string.Empty,
                    ProductCount = c.Products?.Count ?? 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all categories");
                return new List<CategoryInfo>();
            }
        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            try
            {
                return await _productRepository.GetCategoryByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting category by ID: {CategoryId}", id);
                return null;
            }
        }

        public async Task<CategoryInfo> CreateCategoryAsync(CreateCategoryViewModel model)
        {
            try
            {
                var category = new Category
                {
                    Name = model.Name,
                    Description = model.Description ?? string.Empty,
                    ImageUrl = model.ImageUrl ?? string.Empty
                };

                var createdCategory = await _productRepository.CreateCategoryAsync(category);

                return new CategoryInfo
                {
                    Id = createdCategory.Id,
                    Name = createdCategory.Name,
                    ProductCount = 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating category");
                throw new InvalidOperationException("Unable to create category", ex);
            }
        }

        public async Task<bool> UpdateCategoryAsync(int id, EditCategoryViewModel model)
        {
            try
            {
                var existingCategory = await _productRepository.GetCategoryByIdAsync(id);
                if (existingCategory == null)
                    return false;

                existingCategory.Name = model.Name;
                existingCategory.Description = model.Description ?? string.Empty;
                existingCategory.ImageUrl = model.ImageUrl ?? string.Empty;

                await _productRepository.UpdateCategoryAsync(existingCategory);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating category with ID: {CategoryId}", id);
                return false;
            }
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            try
            {
                return await _productRepository.DeleteCategoryAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting category with ID: {CategoryId}", id);
                return false;
            }
        }

        // Brands
        public async Task<IEnumerable<BrandInfo>> GetAllBrandsAsync()
        {
            try
            {
                var brands = await _productRepository.GetAllBrandsAsync();
                return brands.Select(b => new BrandInfo
                {
                    Id = b.Id,
                    Name = b.Name ?? string.Empty,
                    ProductCount = b.Products?.Count ?? 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all brands");
                return new List<BrandInfo>();
            }
        }

        public async Task<Brand?> GetBrandByIdAsync(int id)
        {
            try
            {
                return await _productRepository.GetBrandByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting brand by ID: {BrandId}", id);
                return null;
            }
        }

        public async Task<BrandInfo> CreateBrandAsync(CreateBrandViewModel model)
        {
            try
            {
                var brand = new Brand
                {
                    Name = model.Name
                };

                var createdBrand = await _productRepository.CreateBrandAsync(brand);

                return new BrandInfo
                {
                    Id = createdBrand.Id,
                    Name = createdBrand.Name,
                    ProductCount = 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating brand");
                throw new InvalidOperationException("Unable to create brand", ex);
            }
        }

        public async Task<bool> UpdateBrandAsync(int id, EditBrandViewModel model)
        {
            try
            {
                var existingBrand = await _productRepository.GetBrandByIdAsync(id);
                if (existingBrand == null)
                    return false;

                existingBrand.Name = model.Name;

                await _productRepository.UpdateBrandAsync(existingBrand);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating brand with ID: {BrandId}", id);
                return false;
            }
        }

        public async Task<bool> DeleteBrandAsync(int id)
        {
            try
            {
                return await _productRepository.DeleteBrandAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting brand with ID: {BrandId}", id);
                return false;
            }
        }

        // Suppliers
        public async Task<IEnumerable<SupplierInfo>> GetAllSuppliersAsync()
        {
            try
            {
                var suppliers = await _productRepository.GetAllSuppliersAsync();
                return suppliers.Select(s => new SupplierInfo
                {
                    Id = s.Id,
                    Name = s.Name ?? string.Empty,
                    ContactInfo = s.ContactInfo ?? string.Empty
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all suppliers");
                return new List<SupplierInfo>();
            }
        }

        public async Task<Supplier?> GetSupplierByIdAsync(int id)
        {
            try
            {
                return await _productRepository.GetSupplierByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting supplier by ID: {SupplierId}", id);
                return null;
            }
        }

        // Dropdown Data
        public async Task<IEnumerable<Category>> GetCategoriesForDropdownAsync()
        {
            try
            {
                return await _productRepository.GetAllCategoriesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting categories for dropdown");
                return new List<Category>();
            }
        }

        public async Task<IEnumerable<Brand>> GetBrandsForDropdownAsync()
        {
            try
            {
                return await _productRepository.GetAllBrandsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting brands for dropdown");
                return new List<Brand>();
            }
        }

        public async Task<IEnumerable<Supplier>> GetSuppliersForDropdownAsync()
        {
            try
            {
                return await _productRepository.GetAllSuppliersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting suppliers for dropdown");
                return new List<Supplier>();
            }
        }

        public async Task<IEnumerable<ProductVariant>> GetProductVariantsForDropdownAsync()
        {
            try
            {
                var products = await _productRepository.GetAllProductsAsync();
                return products.SelectMany(p => p.Variants ?? new List<ProductVariant>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting product variants for dropdown");
                return new List<ProductVariant>();
            }
        }

        // Validation
        public async Task<bool> ValidateProductDataAsync(CreateProductViewModel model)
        {
            try
            {
                // Check if category exists
                var category = await _productRepository.GetCategoryByIdAsync(model.CategoryId);
                if (category == null)
                    return false;

                // Check if brand exists
                var brand = await _productRepository.GetBrandByIdAsync(model.BrandId);
                if (brand == null)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while validating product data");
                return false;
            }
        }

        public async Task<bool> ValidateProductUpdateAsync(int id, EditProductViewModel model)
        {
            try
            {
                // Check if product exists
                if (!await _productRepository.ProductExistsAsync(id))
                    return false;

                // Check if category exists
                var category = await _productRepository.GetCategoryByIdAsync(model.CategoryId);
                if (category == null)
                    return false;

                // Check if brand exists
                var brand = await _productRepository.GetBrandByIdAsync(model.BrandId);
                if (brand == null)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while validating product update data for ID: {ProductId}", id);
                return false;
            }
        }

        public async Task<bool> CanDeleteProductAsync(int id)
        {
            try
            {
                // Check if product exists
                if (!await _productRepository.ProductExistsAsync(id))
                    return false;

                // Add business rules for deletion (e.g., check if product has orders, etc.)
                // For now, allow deletion
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking if product can be deleted: {ProductId}", id);
                return false;
            }
        }
    }
}