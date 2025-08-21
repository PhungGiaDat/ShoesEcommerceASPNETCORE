using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoesEcommerce.Data;
using ShoesEcommerce.Models.Carts;
using ShoesEcommerce.Models.Products;
using System.Linq;

namespace ShoesEcommerce.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CartController> _logger;

        public CartController(AppDbContext context, ILogger<CartController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Cart
        public async Task<IActionResult> Index()
        {
            try
            {
                var customerId = GetCurrentCustomerId();
                var sessionId = HttpContext.Session.Id;

                _logger.LogInformation("Loading cart for customer {CustomerId} or session {SessionId}", customerId, sessionId);

                Cart? cart;
                if (customerId != 0)
                {
                    // Nếu đã đăng nhập, tìm Cart thông qua Customer.CartId
                    var customer = await _context.Customers
                        .Include(c => c.Cart)
                            .ThenInclude(cart => cart != null ? cart.CartItems : null)
                                .ThenInclude(ci => ci.ProductVariant)
                                    .ThenInclude(pv => pv.Product)
                                        .ThenInclude(p => p.Brand)
                        .Include(c => c.Cart)
                            .ThenInclude(cart => cart != null ? cart.CartItems : null)
                                .ThenInclude(ci => ci.ProductVariant)
                                    .ThenInclude(pv => pv.CurrentStock)
                        .FirstOrDefaultAsync(c => c.Id == customerId);
                    
                    cart = customer?.Cart;
                    
                    // If customer doesn't have a cart yet (edge case), create one
                    if (cart == null && customer != null)
                    {
                        _logger.LogInformation("Creating cart for customer {CustomerId} who doesn't have one yet", customerId);
                        cart = new Cart
                        {
                            SessionId = Guid.NewGuid().ToString(),
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            CartItems = new List<CartItem>()
                        };
                        _context.Carts.Add(cart);
                        await _context.SaveChangesAsync();
                        
                        // Link cart to customer
                        customer.CartId = cart.Id;
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation("Created and linked cart {CartId} to customer {CustomerId}", cart.Id, customerId);
                    }
                }
                else
                {
                    // Nếu chưa đăng nhập, tìm Cart theo SessionId
                    cart = await _context.Carts
                        .Include(c => c.CartItems)
                            .ThenInclude(ci => ci.ProductVariant)
                                .ThenInclude(pv => pv.Product)
                                    .ThenInclude(p => p.Brand)
                        .Include(c => c.CartItems)
                            .ThenInclude(ci => ci.ProductVariant)
                                .ThenInclude(pv => pv.CurrentStock)
                        .FirstOrDefaultAsync(c => c.SessionId == sessionId);
                }

                // Ensure we always return a List<CartItem> to prevent type mismatch
                var cartItems = cart?.CartItems?.ToList() ?? new List<CartItem>();

                _logger.LogInformation("Cart loaded successfully with {CartItemCount} items for customer {CustomerId} or session {SessionId}", 
                    cartItems.Count, customerId, sessionId);

                ViewData["Title"] = "Giỏ hàng";
                return View(cartItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cart for customer {CustomerId} or session {SessionId}", 
                    GetCurrentCustomerId(), HttpContext.Session.Id);
                TempData["Error"] = "Có lỗi xảy ra khi tải giỏ hàng. Vui lòng thử lại.";
                
                ViewData["Title"] = "Giỏ hàng";
                return View(new List<CartItem>());
            }
        }

        // POST: Cart/AddToCart
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productVariantId)
        {
            try
            {
                if (productVariantId == 0)
                {
                    _logger.LogWarning("Invalid product variant ID {ProductVariantId} provided to AddToCart", productVariantId);
                    TempData["Error"] = "ID biến thể sản phẩm không hợp lệ.";
                    return BadRequest("ID biến thể sản phẩm không hợp lệ.");
                }

                var customerId = GetCurrentCustomerId();
                var sessionId = HttpContext.Session.Id;

                _logger.LogInformation("Adding product variant {ProductVariantId} to cart for customer {CustomerId} or session {SessionId}", 
                    productVariantId, customerId, sessionId);

                var productVariant = await _context.ProductVariants
                    .Include(pv => pv.Product)
                    .Include(pv => pv.CurrentStock)
                    .FirstOrDefaultAsync(pv => pv.Id == productVariantId);

                if (productVariant == null)
                {
                    _logger.LogWarning("Product variant {ProductVariantId} not found for AddToCart", productVariantId);
                    TempData["Error"] = "Biến thể sản phẩm không tồn tại.";
                    return NotFound("Biến thể sản phẩm không tồn tại.");
                }

                // Check stock availability using the correct property
                if (productVariant.AvailableQuantity <= 0)
                {
                    _logger.LogWarning("Product variant {ProductVariantId} is out of stock", productVariantId);
                    TempData["Error"] = "Sản phẩm đã hết hàng.";
                    return Json(new { success = false, message = "Sản phẩm đã hết hàng." });
                }

                Cart? cart;
                if (customerId != 0)
                {
                    // Nếu đã đăng nhập, tìm Cart thông qua Customer.CartId
                    var customer = await _context.Customers
                        .Include(c => c.Cart)
                            .ThenInclude(cart => cart.CartItems)
                        .FirstOrDefaultAsync(c => c.Id == customerId);
                    
                    cart = customer?.Cart;
                }
                else
                {
                    // Nếu chưa đăng nhập, tìm Cart theo SessionId
                    cart = await _context.Carts
                        .Include(c => c.CartItems)
                        .FirstOrDefaultAsync(c => c.SessionId == sessionId);
                }

                if (cart == null)
                {
                    _logger.LogInformation("Creating new cart for customer {CustomerId} or session {SessionId}", customerId, sessionId);
                    
                    cart = new Cart
                    {
                        SessionId = sessionId,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                    
                    // Nếu đã đăng nhập, cập nhật Customer.CartId
                    if (customerId != 0)
                    {
                        var customer = await _context.Customers.FindAsync(customerId);
                        if (customer != null)
                        {
                            customer.CartId = cart.Id;
                            await _context.SaveChangesAsync();
                            _logger.LogInformation("Updated customer {CustomerId} CartId to {CartId}", customerId, cart.Id);
                        }
                    }
                }

                var cartItem = await _context.CartItems
                    .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ProductVarientId == productVariantId);

                if (cartItem != null)
                {
                    // Check if adding one more would exceed stock using the correct property
                    if (cartItem.Quantity + 1 > productVariant.AvailableQuantity)
                    {
                        _logger.LogWarning("Cannot add more of product variant {ProductVariantId} - would exceed stock limit", productVariantId);
                        TempData["Error"] = "Không thể thêm sản phẩm này. Số lượng vượt quá hàng tồn kho.";
                        return Json(new { success = false, message = "Không thể thêm sản phẩm này. Số lượng vượt quá hàng tồn kho." });
                    }
                    
                    cartItem.Quantity += 1;
                    _logger.LogInformation("Updated quantity for existing cart item to {Quantity}", cartItem.Quantity);
                }
                else
                {
                    cartItem = new CartItem
                    {
                        CartId = cart.Id,
                        ProductVarientId = productVariantId,
                        Quantity = 1
                    };
                    _context.CartItems.Add(cartItem);
                    _logger.LogInformation("Added new cart item for product variant {ProductVariantId}", productVariantId);
                }

                cart.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Product variant {ProductVariantId} successfully added to cart for customer {CustomerId}", 
                    productVariantId, customerId);

                TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product variant {ProductVariantId} to cart for customer {CustomerId}", 
                    productVariantId, GetCurrentCustomerId());
                TempData["Error"] = "Có lỗi xảy ra khi thêm sản phẩm vào giỏ hàng. Vui lòng thử lại.";
                return RedirectToAction(nameof(Index));
            }
        }

        private int GetCurrentCustomerId()
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                    return 0;

                if (int.TryParse(userIdClaim, out int customerId))
                    return customerId;

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current customer ID");
                return 0;
            }
        }

        // GET: Cart/RemoveFromCart
        [HttpGet]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            try
            {
                _logger.LogInformation("Removing cart item {CartItemId}", id);

                var cartItem = await _context.CartItems.FindAsync(id);
                if (cartItem != null)
                {
                    var cart = await _context.Carts.FindAsync(cartItem.CartId);
                    _context.CartItems.Remove(cartItem);
                    await _context.SaveChangesAsync();

                    if (cart != null)
                    {
                        cart.UpdatedAt = DateTime.Now;
                        await _context.SaveChangesAsync();
                    }

                    _logger.LogInformation("Cart item {CartItemId} removed successfully", id);
                    TempData["Success"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
                }
                else
                {
                    _logger.LogWarning("Cart item {CartItemId} not found for removal", id);
                    TempData["Error"] = "Không tìm thấy sản phẩm trong giỏ hàng.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cart item {CartItemId}", id);
                TempData["Error"] = "Có lỗi xảy ra khi xóa sản phẩm khỏi giỏ hàng. Vui lòng thử lại.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Cart/UpdateQuantity
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int id, int quantity)
        {
            try
            {
                if (quantity < 1)
                {
                    _logger.LogWarning("Invalid quantity {Quantity} provided for cart item {CartItemId}", quantity, id);
                    TempData["Error"] = "Số lượng phải lớn hơn 0.";
                    return BadRequest("Số lượng phải lớn hơn 0.");
                }

                _logger.LogInformation("Updating quantity for cart item {CartItemId} to {Quantity}", id, quantity);

                var cartItem = await _context.CartItems
                    .Include(ci => ci.ProductVariant)
                        .ThenInclude(pv => pv.CurrentStock)
                    .FirstOrDefaultAsync(ci => ci.Id == id);

                if (cartItem != null)
                {
                    // Check if requested quantity exceeds stock using the correct property
                    if (cartItem.ProductVariant != null && quantity > cartItem.ProductVariant.AvailableQuantity)
                    {
                        _logger.LogWarning("Requested quantity {Quantity} exceeds stock {AvailableQuantity} for cart item {CartItemId}", 
                            quantity, cartItem.ProductVariant.AvailableQuantity, id);
                        TempData["Error"] = $"Số lượng yêu cầu vượt quá hàng tồn kho. Chỉ còn {cartItem.ProductVariant.AvailableQuantity} sản phẩm.";
                        return Json(new { success = false, message = $"Chỉ còn {cartItem.ProductVariant.AvailableQuantity} sản phẩm trong kho." });
                    }

                    cartItem.Quantity = quantity;
                    var cart = await _context.Carts.FindAsync(cartItem.CartId);
                    if (cart != null)
                    {
                        cart.UpdatedAt = DateTime.Now;
                        await _context.SaveChangesAsync();
                    }

                    _logger.LogInformation("Cart item {CartItemId} quantity updated to {Quantity}", id, quantity);
                    TempData["Success"] = "Đã cập nhật số lượng sản phẩm.";
                }
                else
                {
                    _logger.LogWarning("Cart item {CartItemId} not found for quantity update", id);
                    TempData["Error"] = "Không tìm thấy sản phẩm trong giỏ hàng.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quantity for cart item {CartItemId}", id);
                TempData["Error"] = "Có lỗi xảy ra khi cập nhật số lượng sản phẩm. Vui lòng thử lại.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Cart/ClearCart - Clear entire cart
        [HttpGet]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                var customerId = GetCurrentCustomerId();
                var sessionId = HttpContext.Session.Id;

                _logger.LogInformation("Clearing cart for customer {CustomerId} or session {SessionId}", customerId, sessionId);

                Cart? cart;
                if (customerId != 0)
                {
                    var customer = await _context.Customers
                        .Include(c => c.Cart)
                            .ThenInclude(cart => cart.CartItems)
                        .FirstOrDefaultAsync(c => c.Id == customerId);
                    
                    cart = customer?.Cart;
                }
                else
                {
                    cart = await _context.Carts
                        .Include(c => c.CartItems)
                        .FirstOrDefaultAsync(c => c.SessionId == sessionId);
                }

                if (cart != null && cart.CartItems.Any())
                {
                    _context.CartItems.RemoveRange(cart.CartItems);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Cart cleared successfully for customer {CustomerId} or session {SessionId}", customerId, sessionId);
                    TempData["Success"] = "Đã xóa tất cả sản phẩm khỏi giỏ hàng.";
                }
                else
                {
                    _logger.LogInformation("No items to clear in cart for customer {CustomerId} or session {SessionId}", customerId, sessionId);
                    TempData["Info"] = "Giỏ hàng đã trống.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart for customer {CustomerId}", GetCurrentCustomerId());
                TempData["Error"] = "Có lỗi xảy ra khi xóa giỏ hàng. Vui lòng thử lại.";
                return RedirectToAction(nameof(Index));
            }
        }

        // API: Cart/GetCartCount - Get cart item count for AJAX calls
        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            try
            {
                var customerId = GetCurrentCustomerId();
                var sessionId = HttpContext.Session.Id;

                Cart? cart;
                if (customerId != 0)
                {
                    var customer = await _context.Customers
                        .Include(c => c.Cart)
                            .ThenInclude(cart => cart.CartItems)
                        .FirstOrDefaultAsync(c => c.Id == customerId);
                    
                    cart = customer?.Cart;
                }
                else
                {
                    cart = await _context.Carts
                        .Include(c => c.CartItems)
                        .FirstOrDefaultAsync(c => c.SessionId == sessionId);
                }

                var count = cart?.CartItems?.Sum(ci => ci.Quantity) ?? 0;
                return Json(new { count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart count");
                return Json(new { count = 0 });
            }
        }
    }
}