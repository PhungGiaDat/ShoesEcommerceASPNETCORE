using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoesEcommerce.Data;
using ShoesEcommerce.Models.Carts;
using ShoesEcommerce.Models.Orders;
using System.Security.Claims;

namespace ShoesEcommerce.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly AppDbContext _context;

        public CheckoutController(AppDbContext context)
        {
            _context = context;
        }

        private int GetCurrentCustomerId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return 0;

            return int.TryParse(userIdClaim, out int customerId) ? customerId : 0;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var customerId = GetCurrentCustomerId();
            var sessionId = HttpContext.Session.Id;

            Cart? cart;
            if (customerId != 0)
            {
                cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.ProductVariant)
                            .ThenInclude(pv => pv.Product)
                    .FirstOrDefaultAsync(c => c.Customer != null && c.Customer.Id == customerId);
            }
            else
            {
                cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.ProductVariant)
                            .ThenInclude(pv => pv.Product)
                    .FirstOrDefaultAsync(c => c.SessionId == sessionId);
            }

            if (cart == null || !cart.CartItems.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index", "Cart");
            }

            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(string paymentMethod, string shippingAddress)
        {
            var customerId = GetCurrentCustomerId();
            var sessionId = HttpContext.Session.Id;

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                .FirstOrDefaultAsync(c => customerId != 0 ? c.Customer.Id == customerId : c.SessionId == sessionId);

            if (cart == null || !cart.CartItems.Any())
            {
                TempData["Error"] = "Giỏ hàng trống.";
                return RedirectToAction("Index", "Cart");
            }

            // 1. Tính tổng tiền
            decimal totalAmount = cart.CartItems.Sum(ci => ci.ProductVariant.Product.Price * ci.Quantity);

            // 2) Tạo Order (đúng schema: CustomerId, ShippingAddressId, CreatedAt, TotalAmount)
            int shippingAddressId;
            if (!int.TryParse(shippingAddress, out shippingAddressId))
            {
                TempData["Error"] = "Địa chỉ giao hàng không hợp lệ.";
                return RedirectToAction("Index", "Checkout");
            }

            var order = new Order
            {
                CustomerId = customerId,               // int, bắt buộc
                ShippingAddressId = shippingAddressId, // int, bắt buộc
                CreatedAt = DateTime.Now,
                TotalAmount = totalAmount,
                OrderDetails = cart.CartItems.Select(ci => new OrderDetail
                {
                    // Bảng CartItems là ProductVarientId (đúng chính tả theo DB)
                    ProductVariantId = ci.ProductVarientId,
                    Quantity = ci.Quantity,
                    // Lưu đơn giá tại thời điểm đặt hàng
                    UnitPrice = ci.ProductVariant.Product.Price
                }).ToList()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // 3. Xóa Cart sau khi đặt hàng
            _context.CartItems.RemoveRange(cart.CartItems);
            _context.Carts.Remove(cart);
            await _context.SaveChangesAsync();

            // 4. Điều hướng sang thanh toán
            if (paymentMethod == "VNPay")
            {
                return RedirectToAction("Pay", "Payment", new { orderId = order.Id, amount = totalAmount });
            }
            else if (paymentMethod == "COD")
            {
                return RedirectToAction("SuccessCOD", "Checkout", new { orderId = order.Id });
            }

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> SuccessCOD(int orderId)
        {
            var order = await _context.Orders.Include(o => o.OrderDetails)
                .ThenInclude(od => od.ProductVariant)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            return View(order);
        }
    }
}
