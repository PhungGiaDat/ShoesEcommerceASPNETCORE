using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ShoesEcommerce.Services;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.ViewModels.Product;
using ShoesEcommerce.Helpers;

namespace ShoesEcommerce.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly IDiscountService _discountService;
        private readonly ICommentService _commentService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(
            IProductService productService,
            IDiscountService discountService,
            ICommentService commentService,
            ILogger<ProductController> logger)
        {
            _productService = productService;
            _discountService = discountService;
            _commentService = commentService;
            _logger = logger;
        }

        // GET: Product - NOW DISPLAYS PRODUCT VARIANTS INSTEAD OF PRODUCTS
        // SEO-friendly URL: /san-pham
        [Route("san-pham")]
        [Route("[controller]")]
        [Route("[controller]/[action]")]
        public async Task<IActionResult> Index(string searchString, int? categoryId, int? brandId, int page = 1, int pageSize = 12)
        {
            ViewData["Title"] = "Danh sách sản phẩm";

            try
            {
                var model = await _productService.GetProductVariantsListAsync(searchString, categoryId, brandId, page, pageSize);
                
                var categories = await _productService.GetCategoriesForDropdownAsync();
                var brands = await _productService.GetBrandsForDropdownAsync();
                
                ViewBag.Categories = categories.Select(c => new { c.Id, c.Name }).ToList();
                ViewBag.Brands = brands.Select(b => new { b.Id, b.Name }).ToList();

                ViewData["CurrentFilter"] = searchString;
                ViewData["CategoryFilter"] = categoryId;
                ViewData["BrandFilter"] = brandId;

                var featuredDiscounts = await _discountService.GetFeaturedDiscountsAsync();
                model.FeaturedDiscounts = featuredDiscounts;

                return View("VariantIndex", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting product variants for user page");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách sản phẩm.";
                
                var emptyModel = new ProductVariantListViewModel
                {
                    ProductVariants = new List<ProductVariantDisplayInfo>(),
                    CurrentPage = page,
                    TotalPages = 0,
                    TotalItems = 0,
                    SearchTerm = searchString
                };
                
                ViewBag.Categories = new List<object>();
                ViewBag.Brands = new List<object>();
                
                return View("VariantIndex", emptyModel);
            }
        }

        // GET: Product/Details/5 - Traditional route (for backward compatibility)
        // GET: /san-pham/{slug} - SEO-friendly route
        [Route("san-pham/{slug}")]
        [Route("product/{slug}")]
        [Route("Product/Details/{id:int}")]
        public async Task<IActionResult> Details(string? slug, int? id, string? color, string? size)
        {
            // Determine product ID from either route parameter or slug
            int productId;
            
            // Case 1: Direct ID access (e.g., /Product/Details/3 or /Product/Details/3?slug=xxx)
            if (id.HasValue)
            {
                productId = id.Value;
            }
            // Case 2: SEO-friendly slug route (e.g., /san-pham/product-name-3)
            else if (!string.IsNullOrEmpty(slug))
            {
                productId = SlugHelper.ExtractIdFromSlug(slug);
            }
            else
            {
                return NotFound();
            }

            if (productId <= 0)
            {
                return NotFound();
            }

            try
            {
                var product = await _productService.GetProductByIdAsync(productId);
                if (product == null)
                {
                    return NotFound("Sản phẩm không tồn tại.");
                }

                // Generate the canonical SEO-friendly slug
                var expectedSlug = product.Name.ToSlugWithId(product.Id);
                
                // Check current path to determine if we need to redirect
                var currentPath = Request.Path.Value?.ToLowerInvariant() ?? "";
                
                // Only redirect if:
                // 1. Accessed via /Product/Details/{id} route (legacy URL)
                // 2. OR accessed via SEO route but with wrong slug
                var isLegacyRoute = currentPath.Contains("/product/details/");
                var isSeoRouteWithWrongSlug = (currentPath.StartsWith("/san-pham/") || currentPath.StartsWith("/product/")) && 
                                               !currentPath.Contains("/product/details/") &&
                                               !string.IsNullOrEmpty(slug) && 
                                               !slug.Equals(expectedSlug, StringComparison.OrdinalIgnoreCase);
                
                if (isLegacyRoute || isSeoRouteWithWrongSlug)
                {
                    // Build the SEO URL directly to avoid any routing issues
                    var seoUrl = $"/san-pham/{expectedSlug}";
                    
                    // Preserve variant query parameters if present
                    var queryParams = new List<string>();
                    if (!string.IsNullOrEmpty(color)) queryParams.Add($"color={Uri.EscapeDataString(color)}");
                    if (!string.IsNullOrEmpty(size)) queryParams.Add($"size={Uri.EscapeDataString(size)}");
                    if (queryParams.Count > 0) seoUrl += "?" + string.Join("&", queryParams);
                    
                    return RedirectPermanent(seoUrl);
                }

                // At this point, we're on the correct SEO URL - render the view
                var variants = await _productService.GetProductVariantsAsync(productId);
                var discountInfo = await _discountService.GetProductDiscountInfoAsync(productId);
                var comments = await _commentService.GetCommentsAsync(productId);
                var qas = await _commentService.GetQAsAsync(productId);

                ViewData["Title"] = product.Name ?? "Chi tiết sản phẩm";
                ViewData["MetaDescription"] = product.Description?.Length > 160 
                    ? product.Description.Substring(0, 157) + "..." 
                    : product.Description;
                ViewData["CanonicalUrl"] = $"{Request.Scheme}://{Request.Host}/san-pham/{expectedSlug}";
                
                ViewBag.Variants = variants;
                ViewBag.DiscountInfo = discountInfo;
                ViewBag.Comments = comments;
                ViewBag.QAs = qas;
                ViewBag.ProductSlug = expectedSlug;
                
                // Pass selected variant info for pre-selection and sharing
                ViewBag.SelectedColor = color;
                ViewBag.SelectedSize = size;

                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting product details for ID: {ProductId}", productId);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin sản phẩm.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: AddComment - Redirect to product index when accessed directly
        [HttpGet]
        public IActionResult AddComment()
        {
            TempData["Info"] = "Vui lòng sử dụng form bình luận trên trang sản phẩm.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(ProductCommentViewModel model)
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Error"] = "Bạn cần đăng nhập để bình luận.";
                return RedirectToProductDetails(model.ProductId);
            }
            
            var customerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(customerIdClaim, out var customerId))
            {
                model.CustomerId = customerId;
            }
            model.CustomerName = User.Identity.Name ?? "Khách hàng";
            
            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToProductDetails(model.ProductId);
            }
            
            await _commentService.AddCommentAsync(model);
            TempData["Success"] = "Đã gửi bình luận thành công.";
            return RedirectToProductDetails(model.ProductId);
        }

        // GET: AddQA - Redirect to product index when accessed directly
        [HttpGet]
        public IActionResult AddQA()
        {
            TempData["Info"] = "Vui lòng sử dụng form hỏi đáp trên trang sản phẩm.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQA(ProductQAViewModel model)
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Error"] = "Bạn cần đăng nhập để gửi câu hỏi.";
                return RedirectToProductDetails(model.ProductId);
            }
            
            var customerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(customerIdClaim, out var customerId))
            {
                model.CustomerId = customerId;
            }
            model.CustomerName = User.Identity.Name ?? "Khách hàng";
            
            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToProductDetails(model.ProductId);
            }
            
            await _commentService.AddQAAsync(model);
            TempData["Success"] = "Đã gửi câu hỏi thành công.";
            return RedirectToProductDetails(model.ProductId);
        }

        // GET: Product/DiscountedProducts - SEO-friendly URL
        [Route("khuyen-mai")]
        [Route("discounted-products")]
        [Route("Product/DiscountedProducts")]
        public async Task<IActionResult> DiscountedProducts(int page = 1, int pageSize = 12)
        {
            ViewData["Title"] = "Sản phẩm khuyến mãi";
            ViewData["MetaDescription"] = "Khám phá các sản phẩm giày dép khuyến mãi với giá ưu đãi tốt nhất. Mua ngay!";

            try
            {
                var discountedVariants = await _productService.GetDiscountedProductVariantsAsync(page, pageSize);
                var featuredDiscounts = await _discountService.GetFeaturedDiscountsAsync();
                
                // Load categories and brands for sidebar filter
                var categories = await _productService.GetCategoriesForDropdownAsync();
                var brands = await _productService.GetBrandsForDropdownAsync();
                
                ViewBag.Categories = categories.Select(c => new { c.Id, c.Name }).ToList();
                ViewBag.Brands = brands.Select(b => new { b.Id, b.Name }).ToList();

                var model = new ProductVariantListViewModel
                {
                    ProductVariants = discountedVariants,
                    FeaturedDiscounts = featuredDiscounts,
                    CurrentPage = page,
                    TotalPages = (int)Math.Ceiling((double)discountedVariants.Count() / pageSize),
                    TotalItems = discountedVariants.Count(),
                    ShowDiscountsOnly = true
                };

                return View("VariantIndex", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting discounted product variants");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải sản phẩm khuyến mãi.";
                return RedirectToAction(nameof(Index));
            }
        }

        // API endpoint for AJAX calls (for search autocomplete, etc.)
        [HttpGet]
        public async Task<IActionResult> Search(string term, int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return Json(new List<object>());
            }

            try
            {
                var searchResult = await _productService.GetProductVariantsListAsync(term, null, null, 1, limit);
                
                var variants = searchResult.ProductVariants.Select(v => new {
                    id = v.Id,
                    productId = v.ProductId,
                    name = v.DisplayName,
                    slug = v.DisplayName.ToSlugWithId(v.ProductId),
                    price = v.Price,
                    discountedPrice = v.DiscountedPrice,
                    imageUrl = v.ImageUrl,
                    categoryName = v.CategoryName,
                    brandName = v.BrandName,
                    color = v.Color,
                    size = v.Size,
                    hasDiscount = v.HasActiveDiscount,
                    discountPercentage = v.DiscountPercentage,
                    stockQuantity = v.StockQuantity,
                    isInStock = v.IsInStock
                }).ToList();

                return Json(variants);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during product variant search for term: {SearchTerm}", term);
                return Json(new List<object>());
            }
        }

        // API endpoint to get variants by category
        [HttpGet]
        public async Task<IActionResult> GetByCategory(int categoryId, int limit = 20)
        {
            try
            {
                var searchResult = await _productService.GetProductVariantsListAsync(null, categoryId, null, 1, limit);
                
                var variants = searchResult.ProductVariants.Select(v => new {
                    id = v.Id,
                    productId = v.ProductId,
                    name = v.DisplayName,
                    slug = v.DisplayName.ToSlugWithId(v.ProductId),
                    price = v.Price,
                    discountedPrice = v.DiscountedPrice,
                    imageUrl = v.ImageUrl,
                    brandName = v.BrandName,
                    color = v.Color,
                    size = v.Size,
                    hasDiscount = v.HasActiveDiscount,
                    discountPercentage = v.DiscountPercentage,
                    stockQuantity = v.StockQuantity,
                    isInStock = v.IsInStock
                }).ToList();

                return Json(variants);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting product variants by category: {CategoryId}", categoryId);
                return Json(new List<object>());
            }
        }

        // API endpoint to get variants by brand
        [HttpGet]
        public async Task<IActionResult> GetByBrand(int brandId, int limit = 20)
        {
            try
            {
                var searchResult = await _productService.GetProductVariantsListAsync(null, null, brandId, 1, limit);
                
                var variants = searchResult.ProductVariants.Select(v => new {
                    id = v.Id,
                    productId = v.ProductId,
                    name = v.DisplayName,
                    slug = v.DisplayName.ToSlugWithId(v.ProductId),
                    price = v.Price,
                    discountedPrice = v.DiscountedPrice,
                    imageUrl = v.ImageUrl,
                    categoryName = v.CategoryName,
                    color = v.Color,
                    size = v.Size,
                    hasDiscount = v.HasActiveDiscount,
                    discountPercentage = v.DiscountPercentage,
                    stockQuantity = v.StockQuantity,
                    isInStock = v.IsInStock
                }).ToList();

                return Json(variants);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting product variants by brand: {BrandId}", brandId);
                return Json(new List<object>());
            }
        }

        // API endpoint to get product variants by product ID (for Add to Cart modal)
        [HttpGet]
        [Route("Product/GetVariants/{productId:int}")]
        public async Task<IActionResult> GetVariants(int productId)
        {
            try
            {
                var variants = await _productService.GetProductVariantsAsync(productId);
                
                var variantList = variants.Select(v => new {
                    id = v.Id,
                    productId = v.ProductId,
                    color = v.Color,
                    size = v.Size,
                    price = v.Price,
                    imageUrl = v.ImageUrl,
                    stockQuantity = v.StockQuantity,
                    availableQuantity = v.StockQuantity,
                    isInStock = v.StockQuantity > 0
                }).ToList();

                return Json(variantList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting product variants for product: {ProductId}", productId);
                return Json(new List<object>());
            }
        }

        // API endpoint to check discount for a product
        [HttpGet]
        public async Task<IActionResult> GetDiscountInfo(int productId)
        {
            try
            {
                var discountInfo = await _discountService.GetProductDiscountInfoAsync(productId);
                
                if (discountInfo == null)
                {
                    return Json(new { hasDiscount = false });
                }

                var result = new {
                    hasDiscount = discountInfo.HasActiveDiscount,
                    discountName = discountInfo.ActiveDiscount?.Name,
                    discountCode = discountInfo.ActiveDiscount?.Code,
                    discountPercentage = discountInfo.DiscountPercentage,
                    discountAmount = discountInfo.DiscountAmount,
                    originalPrice = discountInfo.OriginalPrice,
                    discountedPrice = discountInfo.DiscountedPrice
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting discount info for product: {ProductId}", productId);
                return Json(new { hasDiscount = false });
            }
        }

        /// <summary>
        /// Helper method to redirect to product details using SEO-friendly URL
        /// </summary>
        private async Task<IActionResult> RedirectToProductDetailsAsync(int productId)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(productId);
                if (product != null)
                {
                    var slug = product.Name.ToSlugWithId(product.Id);
                    return RedirectPermanent($"/san-pham/{slug}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product for redirect: {ProductId}", productId);
            }
            
            // Fallback to old route if product not found
            return RedirectToAction("Details", new { id = productId });
        }

        /// <summary>
        /// Synchronous version for simpler redirects
        /// </summary>
        private IActionResult RedirectToProductDetails(int productId)
        {
            // Use a simple redirect that will be handled by the Details action
            return Redirect($"/Product/Details/{productId}");
        }
    }
}