using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.ViewModels.Product;

namespace ShoesEcommerce.Controllers.Admin
{
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly IStockService _stockService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IProductService productService, IStockService stockService, ILogger<ProductController> logger)
        {
            _productService = productService;
            _stockService = stockService;
            _logger = logger;
        }

        // GET: Admin/Product
        public async Task<IActionResult> Index(string searchTerm, int? categoryId, int? brandId, int page = 1, int pageSize = 10)
        {
            ViewData["Title"] = "Quản lý Sản phẩm";

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
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách sản phẩm: " + ex.Message;
                
                var emptyViewModel = new ProductListViewModel();
                ViewBag.Categories = new SelectList(new List<object>(), "Id", "Name");
                ViewBag.Brands = new SelectList(new List<object>(), "Id", "Name");
                return View(emptyViewModel);
            }
        }

        // GET: Admin/Product/Details/5
        public async Task<IActionResult> Details(int id)
        {
            ViewData["Title"] = "Chi tiết Sản phẩm";

            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy sản phẩm!";
                    return RedirectToAction(nameof(Index));
                }

                var variants = await _productService.GetProductVariantsAsync(id);
                ViewBag.Variants = variants;

                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product details for ID: {ProductId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin sản phẩm: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Product/Create
        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Thêm Sản phẩm mới";

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
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải form thêm sản phẩm: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateProductViewModel model)
        {
            ViewData["Title"] = "Thêm Sản phẩm mới";

            if (ModelState.IsValid)
            {
                try
                {
                    var createdProduct = await _productService.CreateProductAsync(model);
                    TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";
                    return RedirectToAction(nameof(Details), new { id = createdProduct.Id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating product");
                    ModelState.AddModelError("", "Có lỗi xảy ra khi thêm sản phẩm: " + ex.Message);
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
                ModelState.AddModelError("", "Không thể tải danh sách: " + ex.Message);
                ViewBag.Categories = new SelectList(new List<object>(), "Id", "Name");
                ViewBag.Brands = new SelectList(new List<object>(), "Id", "Name");
            }

            return View(model);
        }

        // GET: Admin/Product/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Chỉnh sửa Sản phẩm";

            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy sản phẩm!";
                    return RedirectToAction(nameof(Index));
                }

                var model = new EditProductViewModel
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
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
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải form chỉnh sửa: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditProductViewModel model)
        {
            ViewData["Title"] = "Chỉnh sửa Sản phẩm";

            if (id != model.Id)
            {
                TempData["ErrorMessage"] = "ID không hợp lệ!";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _productService.UpdateProductAsync(id, model);
                    if (result)
                    {
                        TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
                        return RedirectToAction(nameof(Details), new { id });
                    }
                    else
                    {
                        ModelState.AddModelError("", "Không thể cập nhật sản phẩm. Vui lòng thử lại.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating product with ID: {ProductId}", id);
                    ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật sản phẩm: " + ex.Message);
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
            ViewData["Title"] = "Xóa Sản phẩm";

            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy sản phẩm!";
                    return RedirectToAction(nameof(Index));
                }

                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product delete page for ID: {ProductId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
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
                    TempData["SuccessMessage"] = "Xóa sản phẩm thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể xóa sản phẩm. Vui lòng thử lại.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product with ID: {ProductId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa sản phẩm: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // === CATEGORIES ===

        // GET: Admin/Product/Categories
        public async Task<IActionResult> Categories()
        {
            ViewData["Title"] = "Quản lý Danh mục";

            try
            {
                var categories = await _productService.GetAllCategoriesAsync();
                return View(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories page");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách danh mục: " + ex.Message;
                return View(new List<CategoryInfo>());
            }
        }

        // GET: Admin/Product/CreateCategory
        public IActionResult CreateCategory()
        {
            ViewData["Title"] = "Thêm Danh mục mới";
            return View(new CreateCategoryViewModel());
        }

        // POST: Admin/Product/CreateCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(CreateCategoryViewModel model)
        {
            ViewData["Title"] = "Thêm Danh mục mới";

            if (ModelState.IsValid)
            {
                try
                {
                    var createdCategory = await _productService.CreateCategoryAsync(model);
                    TempData["SuccessMessage"] = "Thêm danh mục thành công!";
                    return RedirectToAction(nameof(Categories));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating category");
                    ModelState.AddModelError("", "Có lỗi xảy ra khi thêm danh mục: " + ex.Message);
                }
            }

            return View(model);
        }

        // === BRANDS ===

        // GET: Admin/Product/Brands
        public async Task<IActionResult> Brands()
        {
            ViewData["Title"] = "Quản lý Thương hiệu";

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
            ViewData["Title"] = "Thêm Thương hiệu mới";
            return View(new CreateBrandViewModel());
        }

        // POST: Admin/Product/CreateBrand
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBrand(CreateBrandViewModel model)
        {
            ViewData["Title"] = "Thêm Thương hiệu mới";

            if (ModelState.IsValid)
            {
                try
                {
                    var createdBrand = await _productService.CreateBrandAsync(model);
                    TempData["SuccessMessage"] = "Thêm thương hiệu thành công!";
                    return RedirectToAction(nameof(Brands));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating brand");
                    ModelState.AddModelError("", "Có lỗi xảy ra khi thêm thương hiệu: " + ex.Message);
                }
            }

            return View(model);
        }

        // ===== SUPPLIER MANAGEMENT =====
        [HttpGet]
        public async Task<IActionResult> Suppliers()
        {
            ViewData["Title"] = "Quản lý Nhà cung cấp";
            
            try
            {
                var suppliers = await _productService.GetAllSuppliersAsync();
                return View(suppliers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading suppliers");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách nhà cung cấp";
                return View(new List<SupplierInfo>());
            }
        }

        [HttpGet]
        public IActionResult CreateSupplier()
        {
            ViewData["Title"] = "Thêm Nhà cung cấp mới";
            return View(new CreateSupplierViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSupplier(CreateSupplierViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var supplier = await _productService.CreateSupplierAsync(model);
                TempData["SuccessMessage"] = $"Nhà cung cấp '{supplier.Name}' đã được tạo thành công";
                return RedirectToAction(nameof(Suppliers));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating supplier: {Name}", model.Name);
                ModelState.AddModelError("", "Có lỗi xảy ra khi tạo nhà cung cấp: " + ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditSupplier(int id)
        {
            ViewData["Title"] = "Chỉnh sửa Nhà cung cấp";
            
            try
            {
                var supplier = await _productService.GetSupplierByIdAsync(id);
                if (supplier == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy nhà cung cấp";
                    return RedirectToAction(nameof(Suppliers));
                }

                var model = new EditSupplierViewModel
                {
                    Id = supplier.Id,
                    Name = supplier.Name,
                    ContactInfo = supplier.ContactInfo
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading supplier for edit: {Id}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin nhà cung cấp";
                return RedirectToAction(nameof(Suppliers));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSupplier(int id, EditSupplierViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var success = await _productService.UpdateSupplierAsync(id, model);
                if (success)
                {
                    TempData["SuccessMessage"] = "Cập nhật nhà cung cấp thành công";
                    return RedirectToAction(nameof(Suppliers));
                }
                else
                {
                    ModelState.AddModelError("", "Không thể cập nhật nhà cung cấp");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating supplier: {Id}", id);
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật nhà cung cấp: " + ex.Message);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            try
            {
                var success = await _productService.DeleteSupplierAsync(id);
                if (success)
                {
                    TempData["SuccessMessage"] = "Xóa nhà cung cấp thành công";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể xóa nhà cung cấp";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting supplier: {Id}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa nhà cung cấp: " + ex.Message;
            }

            return RedirectToAction(nameof(Suppliers));
        }

        // ===== HELPER METHODS =====
    }
}