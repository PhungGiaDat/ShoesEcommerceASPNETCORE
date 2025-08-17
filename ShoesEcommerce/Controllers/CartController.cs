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

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Cart
        public async Task<IActionResult> Index()
        {
            var customerId = GetCurrentCustomerId();
            var sessionId = HttpContext.Session.Id;

            Cart? cart;
            if (customerId != 0)
            {
                // Nếu đã đăng nhập, tìm Cart thông qua Customer.CartId
                var customer = await _context.Customers
                    .Include(c => c.Cart)
                        .ThenInclude(cart => cart.CartItems)
                            .ThenInclude(ci => ci.ProductVariant)
                                .ThenInclude(pv => pv.Product)
                                    .ThenInclude(p => p.Brand)
                    .FirstOrDefaultAsync(c => c.Id == customerId);
                
                cart = customer?.Cart;
            }
            else
            {
                // Nếu chưa đăng nhập, tìm Cart theo SessionId
                cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.ProductVariant)
                            .ThenInclude(pv => pv.Product)
                                .ThenInclude(p => p.Brand)
                    .FirstOrDefaultAsync(c => c.SessionId == sessionId);
            }

            var cartItems = cart?.CartItems ?? new List<CartItem>();
            ViewData["Title"] = "Giỏ hàng";
            return View(cartItems);
        }

        // POST: Cart/AddToCart
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productVariantId)
        {
            if (productVariantId == 0)
            {
                return BadRequest("ID biến thể sản phẩm không hợp lệ.");
            }

            var customerId = GetCurrentCustomerId();
            var sessionId = HttpContext.Session.Id;
            var productVariant = await _context.ProductVariants
                .Include(pv => pv.Product)
                .FirstOrDefaultAsync(pv => pv.Id == productVariantId);
            if (productVariant == null)
            {
                return NotFound("Biến thể sản phẩm không tồn tại.");
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
                    }
                }
            }

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ProductVarientId == productVariantId);

            if (cartItem != null)
            {
                cartItem.Quantity += 1;
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
            }

            cart.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private int GetCurrentCustomerId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return 0;

            if (int.TryParse(userIdClaim, out int customerId))
                return customerId;

            return 0;
        }

        // GET: Cart/RemoveFromCart
        [HttpGet]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
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
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Cart/UpdateQuantity
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int id, int quantity)
        {
            if (quantity < 1)
            {
                return BadRequest("Số lượng phải lớn hơn 0.");
            }

            var cartItem = await _context.CartItems.FindAsync(id);
            if (cartItem != null)
            {
                cartItem.Quantity = quantity;
                var cart = await _context.Carts.FindAsync(cartItem.CartId);
                if (cart != null)
                {
                    cart.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}