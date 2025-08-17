using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoesEcommerce.Data;
using ShoesEcommerce.Models.Products;
using System.Linq;

namespace ShoesEcommerce.Controllers
{
    public class ProductController : Controller
    {
        private readonly AppDbContext _context;

        public ProductController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Product
        public async Task<IActionResult> Index(string searchString, int? categoryId, int? brandId, int page = 1, int pageSize = 12)
        {
            ViewData["Title"] = "Danh sách sản phẩm";

            // Base query với Include để load Brand, Category và Variants
            var query = _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .AsQueryable();

            // Filter by search string
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p => p.Name.Contains(searchString) ||
                                       p.Description.Contains(searchString));
                ViewData["CurrentFilter"] = searchString;
            }

            // Filter by category
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
                ViewData["CategoryFilter"] = categoryId;
            }

            // Filter by brand
            if (brandId.HasValue)
            {
                query = query.Where(p => p.BrandId == brandId.Value);
                ViewData["BrandFilter"] = brandId;
            }

            // Get data for dropdowns
            ViewBag.Categories = await _context.Categories
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            ViewBag.Brands = await _context.Brands
                .Select(b => new { b.Id, b.Name })
                .ToListAsync();

            // Pagination
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;

            // Get products for current page
            var products = await query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(products);
        }

        // GET: Product/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .Include(p => p.Comments)
                .Include(p => p.QAs)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound("Sản phẩm không tồn tại.");
            }

            ViewData["Title"] = product.Name ?? "Chi tiết sản phẩm";
            return View(product);
        }

        // GET: Product/Create
        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Thêm sản phẩm mới";

            // Load categories and brands for dropdowns
            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Brands = await _context.Brands.ToListAsync();

            return View();
        }

        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,CategoryId,BrandId,ImageUrl,Price")] Product product)
        {
            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Sản phẩm đã được thêm thành công!";
                return RedirectToAction(nameof(Index));
            }

            // Reload dropdowns if validation fails
            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Brands = await _context.Brands.ToListAsync();
            ViewData["Title"] = "Thêm sản phẩm mới";

            return View(product);
        }

        // GET: Product/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            ViewData["Title"] = $"Sửa sản phẩm: {product.Name}";

            // Load categories and brands for dropdowns
            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Brands = await _context.Brands.ToListAsync();

            return View(product);
        }

        // POST: Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,CategoryId,BrandId,ImageUrl,Price")] Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Sản phẩm đã được cập nhật thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            // Reload dropdowns if validation fails
            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Brands = await _context.Brands.ToListAsync();
            ViewData["Title"] = $"Sửa sản phẩm: {product.Name}";

            return View(product);
        }

        // GET: Product/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            ViewData["Title"] = $"Xóa sản phẩm: {product.Name}";
            return View(product);
        }

        // POST: Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Sản phẩm đã được xóa thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        // Helper method to check if product exists
        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

        // API endpoint for AJAX calls (for search autocomplete, etc.)
        [HttpGet]
        public async Task<IActionResult> Search(string term)
        {
            var products = await _context.Products
                .Where(p => p.Name.Contains(term))
                .Select(p => new {
                    id = p.Id,
                    name = p.Name,
                    price = p.Price,
                    imageUrl = p.ImageUrl
                })
                .Take(10)
                .ToListAsync();

            return Json(products);
        }

        // API endpoint to get products by category
        [HttpGet]
        public async Task<IActionResult> GetByCategory(int categoryId)
        {
            var products = await _context.Products
                .Include(p => p.Brand)
                .Where(p => p.CategoryId == categoryId)
                .Select(p => new {
                    id = p.Id,
                    name = p.Name,
                    price = p.Price,
                    imageUrl = p.ImageUrl,
                    brandName = p.Brand.Name
                })
                .ToListAsync();

            return Json(products);
        }

        // API endpoint to get products by brand
        [HttpGet]
        public async Task<IActionResult> GetByBrand(int brandId)
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.BrandId == brandId)
                .Select(p => new {
                    id = p.Id,
                    name = p.Name,
                    price = p.Price,
                    imageUrl = p.ImageUrl,
                    categoryName = p.Category.Name
                })
                .ToListAsync();

            return Json(products);
        }
    }
}