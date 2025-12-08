using Microsoft.AspNetCore.Mvc;
using ShoesEcommerce.Services.Interfaces;
using System.Security.Claims;

namespace ShoesEcommerce.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ICheckoutService _checkoutService;
        private readonly IDiscountService _discountService;
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(
            ICheckoutService checkoutService,
            IDiscountService discountService,
            ILogger<CheckoutController> logger)
        {
            _checkoutService = checkoutService;
            _discountService = discountService;
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

        private string GetCustomerEmail()
        {
            return User.Identity?.Name ?? "guest@temp.com";
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var customerId = GetCurrentCustomerId();
                var sessionId = HttpContext.Session.Id;

                _logger.LogInformation("Loading checkout page for customer {CustomerId} or session {SessionId}", 
                    customerId, sessionId);

                // Validate checkout prerequisites
                var (isValid, errorMessage) = await _checkoutService.ValidateCheckoutAsync(customerId, sessionId);
                if (!isValid)
                {
                    TempData["Error"] = errorMessage;
                    return RedirectToAction("Index", "Cart");
                }

                // Get cart
                var cart = await _checkoutService.GetCartForCheckoutAsync(customerId, sessionId);
                if (cart == null)
                {
                    TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                    return RedirectToAction("Index", "Cart");
                }

                // Get addresses for logged-in customers
                if (customerId != 0)
                {
                    var addresses = await _checkoutService.GetCustomerAddressesAsync(customerId);
                    ViewBag.Addresses = addresses;
                }
                else
                {
                    ViewBag.Addresses = new List<ShoesEcommerce.Models.Orders.ShippingAddress>();
                }

                // Load active discounts
                var activeDiscounts = await _discountService.GetFeaturedDiscountsAsync(5);
                ViewBag.ActiveDiscounts = activeDiscounts;

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
        public async Task<IActionResult> PlaceOrder(string paymentMethod, string shippingAddress, string? discountCode)
        {
            try
            {
                var customerId = GetCurrentCustomerId();
                var sessionId = HttpContext.Session.Id;

                _logger.LogInformation("Placing order for customer {CustomerId} with payment method {PaymentMethod}", 
                    customerId, paymentMethod);

                // Validate input
                if (string.IsNullOrWhiteSpace(paymentMethod))
                {
                    TempData["Error"] = "Vui lòng chọn phương thức thanh toán.";
                    return RedirectToAction("Index");
                }

                if (string.IsNullOrWhiteSpace(shippingAddress) || !int.TryParse(shippingAddress, out int shippingAddressId))
                {
                    TempData["Error"] = "Địa chỉ giao hàng không hợp lệ.";
                    return RedirectToAction("Index");
                }

                // ✅ FIX: Get cart and calculate totals BEFORE placing order (cart will be cleared after)
                var cart = await _checkoutService.GetCartForCheckoutAsync(customerId, sessionId);
                if (cart == null)
                {
                    TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                    return RedirectToAction("Index", "Cart");
                }

                var (subtotal, discountAmount, totalAmount) = await _checkoutService.CalculateOrderTotalsAsync(
                    cart, discountCode, GetCustomerEmail());

                _logger.LogInformation(
                    "Calculated order totals: Subtotal={Subtotal}, Discount={Discount}, Total={Total}",
                    subtotal, discountAmount, totalAmount);

                // Place order through service (this will clear the cart)
                var order = await _checkoutService.PlaceOrderAsync(
                    customerId, sessionId, shippingAddressId, paymentMethod, discountCode);

                if (order == null)
                {
                    TempData["Error"] = "Không thể tạo đơn hàng. Vui lòng thử lại.";
                    return RedirectToAction("Index");
                }

                _logger.LogInformation("Order {OrderId} created successfully with total {TotalAmount}", 
                    order.Id, totalAmount);

                // Redirect based on payment method
                if (paymentMethod == "PayPal")
                {
                    return RedirectToAction("PayPalCheckout", "Payment", new 
                    { 
                        orderId = order.Id,
                        subtotal = subtotal,
                        discountAmount = discountAmount,
                        totalAmount = totalAmount
                    });
                }
                else if (paymentMethod == "VNPay")
                {
                    return RedirectToAction("Pay", "Payment", new { orderId = order.Id, amount = totalAmount });
                }
                else if (paymentMethod == "COD")
                {
                    return RedirectToAction("SuccessCOD", new { orderId = order.Id });
                }

                TempData["Success"] = "Đặt hàng thành công!";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error placing order for customer {CustomerId}", GetCurrentCustomerId());
                TempData["Error"] = "Có lỗi xảy ra trong quá trình đặt hàng. Vui lòng thử lại.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// AJAX endpoint for creating order and returning JSON (for PayPal inline integration)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrderAjax([FromForm] string paymentMethod, [FromForm] string shippingAddress, [FromForm] string? discountCode)
        {
            try
            {
                var customerId = GetCurrentCustomerId();
                var sessionId = HttpContext.Session.Id;

                _logger.LogInformation("Creating order via AJAX for customer {CustomerId} with payment method {PaymentMethod}", 
                    customerId, paymentMethod);

                // Validate input
                if (string.IsNullOrWhiteSpace(paymentMethod))
                {
                    return Json(new { success = false, error = "Vui lòng chọn phương thức thanh toán." });
                }

                if (string.IsNullOrWhiteSpace(shippingAddress) || !int.TryParse(shippingAddress, out int shippingAddressId))
                {
                    return Json(new { success = false, error = "Địa chỉ giao hàng không hợp lệ." });
                }

                // Get cart and calculate totals BEFORE placing order
                var cart = await _checkoutService.GetCartForCheckoutAsync(customerId, sessionId);
                if (cart == null)
                {
                    return Json(new { success = false, error = "Giỏ hàng của bạn đang trống." });
                }

                var (subtotal, discountAmount, totalAmount) = await _checkoutService.CalculateOrderTotalsAsync(
                    cart, discountCode, GetCustomerEmail());

                // Place order through service
                var order = await _checkoutService.PlaceOrderAsync(
                    customerId, sessionId, shippingAddressId, paymentMethod, discountCode);

                if (order == null)
                {
                    return Json(new { success = false, error = "Không thể tạo đơn hàng. Vui lòng thử lại." });
                }

                _logger.LogInformation("Order {OrderId} created via AJAX with total {TotalAmount}", 
                    order.Id, totalAmount);

                // Return JSON with order details
                return Json(new 
                { 
                    success = true,
                    orderId = order.Id,
                    subtotal = subtotal,
                    discountAmount = discountAmount,
                    totalAmount = totalAmount,
                    paymentMethod = paymentMethod
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order via AJAX for customer {CustomerId}", GetCurrentCustomerId());
                return Json(new { success = false, error = "Có lỗi xảy ra trong quá trình đặt hàng. Vui lòng thử lại." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddShippingAddress(
            [FromForm] string fullName, [FromForm] string phoneNumber, 
            [FromForm] string address, [FromForm] string city, [FromForm] string district)
        {
            try
            {
                var customerId = GetCurrentCustomerId();
                
                if (customerId == 0)
                {
                    return Json(new { success = false, message = "Bạn cần đăng nhập để thêm địa chỉ." });
                }

                var shippingAddress = await _checkoutService.CreateShippingAddressAsync(
                    customerId, fullName, phoneNumber, address, city, district);

                if (shippingAddress == null)
                {
                    return Json(new { success = false, message = "Vui lòng điền đầy đủ thông tin." });
                }

                _logger.LogInformation("Shipping address {AddressId} created for customer {CustomerId}", 
                    shippingAddress.Id, customerId);

                return Json(new 
                { 
                    success = true, 
                    message = "Thêm địa chỉ thành công!", 
                    address = new 
                    {
                        id = shippingAddress.Id,
                        fullName = shippingAddress.FullName,
                        phoneNumber = shippingAddress.PhoneNumber,
                        address = shippingAddress.Address,
                        city = shippingAddress.City,
                        district = shippingAddress.District
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding shipping address for customer {CustomerId}", GetCurrentCustomerId());
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm địa chỉ. Vui lòng thử lại." });
            }
        }

        public async Task<IActionResult> SuccessCOD(int orderId)
        {
            try
            {
                _logger.LogInformation("Loading COD success page for order {OrderId}", orderId);

                // You may want to add order retrieval through OrderService here
                // For now, just show success page
                ViewBag.OrderId = orderId;

                return View();
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
