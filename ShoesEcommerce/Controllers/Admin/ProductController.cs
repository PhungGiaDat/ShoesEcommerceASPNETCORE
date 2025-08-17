using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.ViewModels.Product;

namespace ShoesEcommerce.Controllers.Admin
{
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IProductService productService, ILogger<ProductController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        // GET: Admin/Product
        public async Task<IActionResult> Index(string searchTerm, int? categoryId, int? brandId, int page = 1, int pageSize = 10)
        {
            ViewData["Title"] = "Qu?n lý S?n ph?m";

            try
            {
                var viewModel = await _productService.GetProductsAsync(searchTerm, categoryId, brandId, page, pageSize);

                // Get data for dropdowns
                var categories = await _productService.GetCategoriesForDropdownAsync();
                var brands = await _productService.GetBrandsForDropdownAsync();

                ViewBag.Categories = new SelectList(categories, "Id", "Name", categoryId);
                ViewBag.Brands = new SelectList(brands, "Id", "Name", brandId);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product index page");
                TempData["ErrorMessage"] = "Có l?i x?y ra khi t?i danh sách s?n ph?m: " + ex.Message;
                
                var emptyViewModel = new ProductListViewModel();
                ViewBag.Categories = new SelectList(new List<object>(), "Id", "Name");
                ViewBag.Brands = new SelectList(new List<object>(), "Id", "Name");
                return View(emptyViewModel);
            }
        }

        // GET: Admin/Product/Details/5
        public async Task<IActionResult> Details(int id)
        {
            ViewData["Title"] = "Chi ti?t S?n ph?m";

            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Không tìm th?y s?n ph?m!";
                    return RedirectToAction(nameof(Index));
                }

                var variants = await _productService.GetProductVariantsAsync(id);
                ViewBag.Variants = variants;

                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product details for ID: {ProductId}", id);
                TempData["ErrorMessage"] = "Có l?i x?y ra khi t?i thông tin s?n ph?m: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Product/Create
        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Thêm S?n ph?m m?i";

            try
            {
                var categories = await _productService.GetCategoriesForDropdownAsync();
                var brands = await _productService.GetBrandsForDropdownAsync();

                ViewBag.Categories = new SelectList(categories, "Id", "Name");
                ViewBag.Brands = new SelectList(brands, "Id", "Name");

                return View(new CreateProductViewModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product create page");
                TempData["ErrorMessage"] = "Có l?i x?y ra khi t?i form thêm s?n ph?m: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateProductViewModel model)
        {
            ViewData["Title"] = "Thêm S?n ph?m m?i";

            if (ModelState.IsValid)
            {
                try
                {
                    var createdProduct = await _productService.CreateProductAsync(model);
                    TempData["SuccessMessage"] = "Thêm s?n ph?m thành công!";
                    return RedirectToAction(nameof(Details), new { id = createdProduct.Id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating product");
                    ModelState.AddModelError("", "Có l?i x?y ra khi thêm s?n ph?m: " + ex.Message);
                }
            }

            // Reload dropdowns if validation fails
            try
            {
                var categories = await _productService.GetCategoriesForDropdownAsync();
                var brands = await _productService.GetBrandsForDropdownAsync();

                ViewBag.Categories = new SelectList(categories, "Id", "Name", model.CategoryId);
                ViewBag.Brands = new SelectList(brands, "Id", "Name", model.BrandId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dropdowns for create form");
                ModelState.AddModelError("", "Không th? t?i danh sách: " + ex.Message);
                ViewBag.Categories = new SelectList(new List<object>(), "Id", "Name");
                ViewBag.Brands = new SelectList(new List<object>(), "Id", "Name");
            }

            return View(model);
        }

        // GET: Admin/Product/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Ch?nh s?a S?n ph?m";

            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Không tìm th?y s?n ph?m!";
                    return RedirectToAction(nameof(Index));
                }

                var model = new EditProductViewModel
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    CategoryId = product.CategoryId,
                    BrandId = product.BrandId
                };

                var categories = await _productService.GetCategoriesForDropdownAsync();
                var brands = await _productService.GetBrandsForDropdownAsync();

                ViewBag.Categories = new SelectList(categories, "Id", "Name", model.CategoryId);
                ViewBag.Brands = new SelectList(brands, "Id", "Name", model.BrandId);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product edit page for ID: {ProductId}", id);
                TempData["ErrorMessage"] = "Có l?i x?y ra khi t?i form ch?nh s?a: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditProductViewModel model)
        {
            ViewData["Title"] = "Ch?nh s?a S?n ph?m";

            if (id != model.Id)
            {
                TempData["ErrorMessage"] = "ID không h?p l?!";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _productService.UpdateProductAsync(id, model);
                    if (result)
                    {
                        TempData["SuccessMessage"] = "C?p nh?t s?n ph?m thành công!";
                        return RedirectToAction(nameof(Details), new { id });
                    }
                    else
                    {
                        ModelState.AddModelError("", "Không th? c?p nh?t s?n ph?m. Vui lòng th? l?i.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating product with ID: {ProductId}", id);
                    ModelState.AddModelError("", "Có l?i x?y ra khi c?p nh?t s?n ph?m: " + ex.Message);
                }
            }

            // Reload dropdowns if validation fails
            try
            {
                var categories = await _productService.GetCategoriesForDropdownAsync();
                var brands = await _productService.GetBrandsForDropdownAsync();

                ViewBag.Categories = new SelectList(categories, "Id", "Name", model.CategoryId);
                ViewBag.Brands = new SelectList(brands, "Id", "Name", model.BrandId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dropdowns for edit form");
                ViewBag.Categories = new SelectList(new List<object>(), "Id", "Name");
                ViewBag.Brands = new SelectList(new List<object>(), "Id", "Name");
            }

            return View(model);
        }

        // GET: Admin/Product/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            ViewData["Title"] = "Xóa S?n ph?m";

            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Không tìm th?y s?n ph?m!";
                    return RedirectToAction(nameof(Index));
                }

                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product delete page for ID: {ProductId}", id);
                TempData["ErrorMessage"] = "Có l?i x?y ra: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var result = await _productService.DeleteProductAsync(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Xóa s?n ph?m thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không th? xóa s?n ph?m. Vui lòng th? l?i.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product with ID: {ProductId}", id);
                TempData["ErrorMessage"] = "Có l?i x?y ra khi xóa s?n ph?m: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // === CATEGORIES ===

        // GET: Admin/Product/Categories
        public async Task<IActionResult> Categories()
        {
            ViewData["Title"] = "Qu?n lý Danh m?c";

            try
            {
                var categories = await _productService.GetAllCategoriesAsync();
                return View(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories page");
                TempData["ErrorMessage"] = "Có l?i x?y ra khi t?i danh sách danh m?c: " + ex.Message;
                return View(new List<CategoryInfo>());
            }
        }

        // GET: Admin/Product/CreateCategory
        public IActionResult CreateCategory()
        {
            ViewData["Title"] = "Thêm Danh m?c m?i";
            return View(new CreateCategoryViewModel());
        }

        // POST: Admin/Product/CreateCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(CreateCategoryViewModel model)
        {
            ViewData["Title"] = "Thêm Danh m?c m?i";

            if (ModelState.IsValid)
            {
                try
                {
                    var createdCategory = await _productService.CreateCategoryAsync(model);
                    TempData["SuccessMessage"] = "Thêm danh m?c thành công!";
                    return RedirectToAction(nameof(Categories));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating category");
                    ModelState.AddModelError("", "Có l?i x?y ra khi thêm danh m?c: " + ex.Message);
                }
            }

            return View(model);
        }

        // === BRANDS ===

        // GET: Admin/Product/Brands
        public async Task<IActionResult> Brands()
        {
            ViewData["Title"] = "Qu?n lý Th??ng hi?u";

            try
            {
                var brands = await _productService.GetAllBrandsAsync();
                return View(brands);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading brands page");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách thương hiệu: " + ex.Message;
                return View(new List<BrandInfo>());
            }
        }

        // GET: Admin/Product/CreateBrand
        public IActionResult CreateBrand()
        {
            ViewData["Title"] = "Thêm Th??ng hi?u m?i";
            return View(new CreateBrandViewModel());
        }

        // POST: Admin/Product/CreateBrand
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBrand(CreateBrandViewModel model)
        {
            ViewData["Title"] = "Thêm Th??ng hi?u m?i";

            if (ModelState.IsValid)
            {
                try
                {
                    var createdBrand = await _productService.CreateBrandAsync(model);
                    TempData["SuccessMessage"] = "Thêm th??ng hi?u thành công!";
                    return RedirectToAction(nameof(Brands));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating brand");
                    ModelState.AddModelError("", "Có l?i x?y ra khi thêm th??ng hi?u: " + ex.Message);
                }
            }

            return View(model);
        }

        // === PRODUCT VARIANTS ===

        // GET: Admin/Product/Variants/5
        public async Task<IActionResult> Variants(int id)
        {
            ViewData["Title"] = "Phiên b?n S?n ph?m";

            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Không tìm th?y s?n ph?m!";
                    return RedirectToAction(nameof(Index));
                }

                var variants = await _productService.GetProductVariantsAsync(id);
                ViewBag.Product = product;
                
                return View(variants);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product variants for product ID: {ProductId}", id);
                TempData["ErrorMessage"] = "Có l?i x?y ra: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Product/CreateVariant/5
        public async Task<IActionResult> CreateVariant(int productId)
        {
            ViewData["Title"] = "Thêm Phiên b?n m?i";

            try
            {
                var product = await _productService.GetProductByIdAsync(productId);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Không tìm th?y s?n ph?m!";
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.Product = product;
                return View(new CreateProductVariantViewModel { ProductId = productId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create variant page for product ID: {ProductId}", productId);
                TempData["ErrorMessage"] = "Có l?i x?y ra: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Product/CreateVariant
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVariant(CreateProductVariantViewModel model)
        {
            ViewData["Title"] = "Thêm Phiên b?n m?i";

            if (ModelState.IsValid)
            {
                try
                {
                    var createdVariant = await _productService.CreateProductVariantAsync(model);
                    TempData["SuccessMessage"] = "Thêm phiên b?n s?n ph?m thành công!";
                    return RedirectToAction(nameof(Variants), new { id = model.ProductId });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating product variant");
                    ModelState.AddModelError("", "Có l?i x?y ra khi thêm phiên b?n: " + ex.Message);
                }
            }

            // Reload product info if validation fails
            try
            {
                var product = await _productService.GetProductByIdAsync(model.ProductId);
                ViewBag.Product = product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product info for create variant form");
                ModelState.AddModelError("", "Không th? t?i thông tin s?n ph?m: " + ex.Message);
            }

            return View(model);
        }
    }
}