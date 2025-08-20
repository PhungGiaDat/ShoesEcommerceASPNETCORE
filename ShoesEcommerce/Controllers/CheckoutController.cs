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
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(AppDbContext context, ILogger<CheckoutController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private int GetCurrentCustomerId()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                    return 0;

                return int.TryParse(userIdClaim, out int customerId) ? customerId : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current customer ID");
                return 0;
            }
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var customerId = GetCurrentCustomerId();
                var sessionId = HttpContext.Session.Id;

                _logger.LogInformation("Loading checkout page for customer {CustomerId} or session {SessionId}", customerId, sessionId);

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
                    _logger.LogWarning("Empty cart during checkout for customer {CustomerId} or session {SessionId}", customerId, sessionId);
                    TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                    return RedirectToAction("Index", "Cart");
                }

                _logger.LogInformation("Checkout page loaded successfully for customer {CustomerId} with {CartItemCount} items", 
                    customerId, cart.CartItems.Count);

                return View(cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading checkout page");
                TempData["Error"] = "Có lỗi xảy ra khi tải trang thanh toán. Vui lòng thử lại.";
                return RedirectToAction("Index", "Cart");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(string paymentMethod, string shippingAddress)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var customerId = GetCurrentCustomerId();
                var sessionId = HttpContext.Session.Id;

                _logger.LogInformation("Placing order for customer {CustomerId} or session {SessionId} with payment method {PaymentMethod}", 
                    customerId, sessionId, paymentMethod);

                // Validate input parameters
                if (string.IsNullOrWhiteSpace(paymentMethod))
                {
                    _logger.LogWarning("Payment method not provided during order placement");
                    TempData["Error"] = "Vui lòng chọn phương thức thanh toán.";
                    return RedirectToAction("Index", "Checkout");
                }

                if (string.IsNullOrWhiteSpace(shippingAddress) || !int.TryParse(shippingAddress, out int shippingAddressId))
                {
                    _logger.LogWarning("Invalid shipping address {ShippingAddress} provided during order placement", shippingAddress);
                    TempData["Error"] = "Địa chỉ giao hàng không hợp lệ.";
                    return RedirectToAction("Index", "Checkout");
                }

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.ProductVariant)
                            .ThenInclude(pv => pv.Product)
                    .FirstOrDefaultAsync(c => customerId != 0 ? c.Customer.Id == customerId : c.SessionId == sessionId);

                if (cart == null || !cart.CartItems.Any())
                {
                    _logger.LogWarning("Empty cart during order placement for customer {CustomerId} or session {SessionId}", customerId, sessionId);
                    TempData["Error"] = "Giỏ hàng trống.";
                    return RedirectToAction("Index", "Cart");
                }

                // Validate stock availability before creating order
                foreach (var cartItem in cart.CartItems)
                {
                    if (cartItem.ProductVariant == null)
                    {
                        _logger.LogError("ProductVariant not found for cart item {CartItemId}", cartItem.Id);
                        TempData["Error"] = "Có sản phẩm trong giỏ hàng không tồn tại.";
                        return RedirectToAction("Index", "Cart");
                    }

                    // Add stock validation here if needed
                    // if (cartItem.ProductVariant.StockQuantity < cartItem.Quantity) { ... }
                }

                // Calculate total amount
                decimal totalAmount = cart.CartItems.Sum(ci => ci.ProductVariant.Price * ci.Quantity); // ✅ FIXED: Use ProductVariant.Price

                _logger.LogInformation("Order total calculated: {TotalAmount} for {CartItemCount} items", totalAmount, cart.CartItems.Count);

                // Create Order
                var order = new Order
                {
                    CustomerId = customerId,               // int, bắt buộc
                    ShippingAddressId = shippingAddressId, // int, bắt buộc
                    CreatedAt = DateTime.Now,
                    TotalAmount = totalAmount,
                    OrderDetails = cart.CartItems.Select(ci => new OrderDetail
                    {
                        ProductVariantId = ci.ProductVarientId,
                        Quantity = ci.Quantity,
                        UnitPrice = ci.ProductVariant.Price // ✅ FIXED: Use ProductVariant.Price
                    }).ToList()
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Order {OrderId} created successfully for customer {CustomerId} with total {TotalAmount}", 
                    order.Id, customerId, totalAmount);

                // Clear cart after successful order creation
                _context.CartItems.RemoveRange(cart.CartItems);
                _context.Carts.Remove(cart);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("Cart cleared successfully after order {OrderId} creation", order.Id);

                // Redirect based on payment method
                if (paymentMethod == "VNPay")
                {
                    _logger.LogInformation("Redirecting to VNPay payment for order {OrderId}", order.Id);
                    return RedirectToAction("Pay", "Payment", new { orderId = order.Id, amount = totalAmount });
                }
                else if (paymentMethod == "COD")
                {
                    _logger.LogInformation("COD payment selected for order {OrderId}", order.Id);
                    return RedirectToAction("SuccessCOD", "Checkout", new { orderId = order.Id });
                }

                _logger.LogWarning("Unknown payment method {PaymentMethod} for order {OrderId}", paymentMethod, order.Id);
                TempData["Success"] = "Đặt hàng thành công!";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error placing order for customer {CustomerId} with payment method {PaymentMethod}", 
                    GetCurrentCustomerId(), paymentMethod);
                TempData["Error"] = "Có lỗi xảy ra trong quá trình đặt hàng. Vui lòng thử lại.";
                return RedirectToAction("Index", "Checkout");
            }
        }

        public async Task<IActionResult> SuccessCOD(int orderId)
        {
            try
            {
                _logger.LogInformation("Loading COD success page for order {OrderId}", orderId);

                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.ProductVariant)
                            .ThenInclude(pv => pv.Product)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    _logger.LogWarning("Order {OrderId} not found for COD success page", orderId);
                    TempData["Error"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction("Index", "Home");
                }

                // Verify order belongs to current customer
                var customerId = GetCurrentCustomerId();
                if (customerId != 0 && order.CustomerId != customerId)
                {
                    _logger.LogWarning("Unauthorized access to order {OrderId} by customer {CustomerId}", orderId, customerId);
                    TempData["Error"] = "Bạn không có quyền truy cập đơn hàng này.";
                    return RedirectToAction("Index", "Home");
                }

                _logger.LogInformation("COD success page loaded for order {OrderId}", orderId);
                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading COD success page for order {OrderId}", orderId);
                TempData["Error"] = "Có lỗi xảy ra khi tải trang xác nhận đơn hàng.";
                return RedirectToAction("Index", "Home");
            }
        }
    }
}
