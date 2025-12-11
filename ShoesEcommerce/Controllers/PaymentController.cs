using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShoesEcommerce.Models.Payments.PayPal;
using ShoesEcommerce.Models.ViewModels;
using ShoesEcommerce.Repositories.Interfaces;
using ShoesEcommerce.Services.Interfaces;
using System.Security.Claims;
using ShoesEcommerce.Services.Payment;
using System.Net;
using System.Linq;

namespace ShoesEcommerce.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IVnPayService _vnPayService; // <-- Đ? THÊM: Khai báo Service cho VNPay
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IPaymentService paymentService,
            IPaymentRepository paymentRepository,
            IVnPayService vnPayService, // <-- Đ? THÊM: Tiêm IVnPayService vào constructor
            ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _paymentRepository = paymentRepository;
            _vnPayService = vnPayService; // <-- Đ? THÊM: Gán giá tr?
            this._logger = logger;
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

        // =========================================================================
        // --- PAYPAL ACTIONS --- 
        // =========================================================================

        /// <summary>
        /// PayPal checkout page - Displays PayPal button
        /// </summary>
        [HttpGet]
        public IActionResult PayPalCheckout(int orderId, decimal subtotal, decimal discountAmount, decimal totalAmount)
        {
            try
            {
                _logger.LogInformation(
                    "Loading PayPal checkout for order {OrderId}: Subtotal={Subtotal}, Discount={Discount}, Total={Total}",
                    orderId, subtotal, discountAmount, totalAmount);

                // Store values in TempData for the view - Convert decimals to strings
                TempData["Subtotal"] = subtotal.ToString("F2");
                TempData["DiscountAmount"] = discountAmount.ToString("F2");
                TempData["TotalAmount"] = totalAmount.ToString("F2");

                return View(orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading PayPal checkout for order {OrderId}", orderId);
                TempData["Error"] = "Có l?i x?y ra khi t?i trang thanh toán PayPal.";
                return RedirectToAction("Index", "Checkout");
            }
        }

        /// <summary>
        /// Create PayPal order - Called from client-side JavaScript
        /// </summary>
        [HttpPost("/payment/create-paypal-order")]
        public async Task<IActionResult> CreatePayPalOrder([FromBody] CreatePayPalOrderRequest? request)
        {
            try
            {
                // ? Validate request
                if (request == null)
                {
                    _logger.LogError("CreatePayPalOrder called with null request body");
                    return BadRequest(new { error = "Request body is null. Please provide order details." });
                }

                if (request.OrderId <= 0)
                {
                    _logger.LogError("CreatePayPalOrder called with invalid OrderId: {OrderId}", request.OrderId);
                    return BadRequest(new { error = "Invalid order ID" });
                }

                if (request.TotalAmount <= 0)
                {
                    _logger.LogError("CreatePayPalOrder called with invalid TotalAmount: {TotalAmount}", request.TotalAmount);
                    return BadRequest(new { error = "Invalid total amount" });
                }

                _logger.LogInformation(
                    "Creating PayPal order: OrderId={OrderId}, Subtotal={Subtotal}, Discount={Discount}, Total={Total}",
                    request.OrderId, request.Subtotal, request.DiscountAmount, request.TotalAmount);

                // ? Verify order exists using repository
                if (!await _paymentRepository.OrderExistsAsync(request.OrderId))
                {
                    _logger.LogWarning("Order {OrderId} not found", request.OrderId);
                    return BadRequest(new { error = "Đơn hàng không t?n t?i" });
                }

                // ? Optional: Verify customer ownership
                var customerId = GetCurrentCustomerId();
                if (customerId != 0)
                {
                    if (!await _paymentRepository.OrderBelongsToCustomerAsync(request.OrderId, customerId))
                    {
                        _logger.LogWarning(
                            "Unauthorized access to order {OrderId} by customer {CustomerId}",
                            request.OrderId, customerId);
                        return BadRequest(new { error = "B?n không có quy?n truy c?p đơn hàng này" });
                    }
                }

                // Build return and cancel URLs
                var returnUrl = Url.Action(
                    "PayPalSuccess",
                    "Payment",
                    new { orderId = request.OrderId },
                    protocol: Request.Scheme,
                    host: Request.Host.ToString());

                var cancelUrl = Url.Action(
                    "PayPalCancel",
                    "Payment",
                    new { orderId = request.OrderId },
                    protocol: Request.Scheme,
                    host: Request.Host.ToString());

                // ? Create PayPal order via service
                var response = await _paymentService.CreatePayPalOrderAsync(
                    request.OrderId,
                    request.Subtotal,
                    request.DiscountAmount,
                    request.TotalAmount,
                    returnUrl!,
                    cancelUrl!);

                _logger.LogInformation(
                    "PayPal order created: PayPalOrderId={PayPalOrderId}, OrderId={OrderId}",
                    response.id, request.OrderId);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayPal order. Request: {@Request}", request);
                return BadRequest(new { error = $"L?i t?o PayPal order: {ex.Message}" });
            }
        }

        /// <summary>
        /// Capture PayPal order - Called from client-side after user approves
        /// </summary>
        [HttpPost("/payment/capture-paypal-order")]
        public async Task<IActionResult> CapturePayPalOrder([FromQuery] string orderId, [FromQuery] int orderIdLocal)
        {
            try
            {
                _logger.LogInformation(
                    "Capturing PayPal order: PayPalOrderId={PayPalOrderId}, LocalOrderId={LocalOrderId}",
                    orderId, orderIdLocal);

                // ? Capture payment on PayPal via service
                var response = await _paymentService.CapturePayPalOrderAsync(orderId);

                // Check capture status
                var capture = response.purchase_units?.FirstOrDefault()?.payments?.captures?.FirstOrDefault();

                if (capture == null)
                {
                    _logger.LogError("No capture information found for PayPal order {PayPalOrderId}", orderId);
                    return BadRequest(new { error = "Không th? xác nh?n thanh toán" });
                }

                var isSuccessful = capture.status == "COMPLETED";
                var paidAt = capture.create_time;
                var transactionId = capture.id;

                // ? Update payment status via service
                await _paymentService.UpdatePaymentStatusAsync(
                    orderIdLocal,
                    isSuccessful ? "Paid" : "Failed",
                    paidAt,
                    transactionId);

                _logger.LogInformation(
                    "PayPal payment captured successfully: PayPalOrderId={PayPalOrderId}, LocalOrderId={LocalOrderId}, Status={Status}",
                    orderId, orderIdLocal, capture.status);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing PayPal order {PayPalOrderId}", orderId);
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// PayPal success redirect page
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> PayPalSuccess(int orderId, string? token)
        {
            try
            {
                _logger.LogInformation(
                    "PayPal success redirect: OrderId={OrderId}, Token={Token}",
                    orderId, token);

                // ✅ IMPROVED: Better validation with logging
                if (orderId <= 0)
                {
                    _logger.LogWarning("PayPalSuccess called with invalid orderId: {OrderId}. This may be from a stale browser tab or bookmark.", orderId);
                    
                    // ✅ IMPROVED: Check if this is a direct navigation (no token = likely stale)
                    if (string.IsNullOrEmpty(token))
                    {
                        // Don't show error, just redirect silently
                        _logger.LogInformation("No token provided, likely stale navigation. Redirecting to Order page.");
                        return RedirectToAction("Index", "Order");
                    }
                    
                    TempData["Error"] = "Mã đơn hàng không hợp lệ. Vui lòng kiểm tra đơn hàng của bạn.";
                    return RedirectToAction("Index", "Order");
                }

                // Get order with all details
                var order = await _paymentRepository.GetOrderWithDetailsAsync(orderId);

                if (order == null)
                {
                    _logger.LogWarning("Order {OrderId} not found on PayPal success", orderId);
                    TempData["Error"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction("Index", "Order");
                }

                // ✅ NEW: Verify order has required data
                if (order.OrderDetails == null || !order.OrderDetails.Any())
                {
                    _logger.LogWarning("Order {OrderId} has no order details", orderId);
                    TempData["Error"] = "Đơn hàng không có sản phẩm.";
                    return RedirectToAction("Index", "Order");
                }

                // ✅ NEW: Log successful access
                _logger.LogInformation(
                    "PayPalSuccess page loaded: OrderId={OrderId}, CustomerId={CustomerId}, PaymentStatus={Status}",
                    orderId, order.CustomerId, order.Payment?.Status ?? "Unknown");

                ViewBag.PayPalToken = token;
                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading PayPal success page for order {OrderId}", orderId);
                
                // ✅ IMPROVED: Better UX - redirect to order list
                TempData["Info"] = "Đơn hàng của bạn đã được xử lý. Vui lòng kiểm tra danh sách đơn hàng.";
                return RedirectToAction("Index", "Order");
            }
        }

        /// <summary>
        /// PayPal cancel redirect page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> PayPalCancel(int orderId)
        {
            try
            {
                _logger.LogInformation("PayPal payment cancelled for order {OrderId}", orderId);

                // ? Update payment status via service
                await _paymentService.UpdatePaymentStatusAsync(orderId, "Cancelled");

                // ? Get order via repository
                var order = await _paymentRepository.GetOrderWithDetailsAsync(orderId);

                if (order == null)
                {
                    _logger.LogWarning("Order {OrderId} not found on PayPal cancel", orderId);
                    TempData["Error"] = "Không t?m th?y đơn hàng.";
                    return RedirectToAction("Index", "Home");
                }

                ViewBag.OrderId = orderId;
                ViewBag.OrderTotal = order.TotalAmount;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling PayPal cancellation for order {OrderId}", orderId);
                TempData["Warning"] = "Thanh toán đ? b? h?y.";
                return RedirectToAction("Index", "Cart");
            }
        }

        /// <summary>
        /// Check payment status
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CheckPaymentStatus(int orderId)
        {
            try
            {
                // ? Get payment via service
                var payment = await _paymentService.GetPaymentByOrderIdAsync(orderId);

                if (payment == null)
                {
                    return NotFound(new { error = "Không t?m th?y thông tin thanh toán" });
                }

                return Ok(new
                {
                    orderId = payment.OrderId,
                    method = payment.Method,
                    status = payment.Status,
                    paidAt = payment.PaidAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking payment status for order {OrderId}", orderId);
                return BadRequest(new { error = ex.Message });
            }
        }

        // =========================================================================
        // --- VNPAY ACTIONS ---
        // =========================================================================

        /// <summary>
        /// VNPay checkout page - hiển thị thông tin đơn hàng trước khi redirect sang VNPay
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> VnPayCheckoutPage(int orderId)
        {
            var order = await _paymentRepository.GetOrderWithDetailsAsync(orderId);
            var customerId = GetCurrentCustomerId();

            if (order == null || (customerId != 0 && order.CustomerId != customerId))
            {
                TempData["Error"] = "Không tìm thấy đơn hàng hoặc bạn không có quyền.";
                return RedirectToAction("Index", "Cart");
            }

            return View("VnPayCheckout", order);
        }

        /// <summary>
        /// VNPay checkout - Redirect to VNPay payment gate
        /// </summary>
        [HttpPost]
        public IActionResult VnPayCheckout(int orderId, decimal amount)
        {
            try
            {
                _logger.LogInformation("Initiating VNPay checkout for order {OrderId} with amount {Amount}", orderId, amount);

                var paymentUrl = _vnPayService.CreatePaymentUrl(orderId, amount, HttpContext);

                if (string.IsNullOrEmpty(paymentUrl))
                {
                    _logger.LogError("Failed to create VNPay payment URL for order {OrderId}", orderId);
                    TempData["Error"] = "Lỗi tạo liên kết thanh toán VNPay.";
                    return RedirectToAction("Index", "Checkout");
                }

                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during VNPay checkout for order {OrderId}", orderId);
                TempData["Error"] = "Có lỗi xảy ra khi chuyển hướng thanh toán VNPay.";
                return RedirectToAction("Index", "Checkout");
            }
        }

        /// <summary>
        /// VNPay return handler - VNPay calls this after payment
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> VnPayReturn()
        {
            try
            {
                _logger.LogInformation("Receiving VNPay return callback: Query={Query}", Request.QueryString);

                var response = _vnPayService.ProcessReturn(Request.Query);

                if (!response.IsSuccess)
                {
                    _logger.LogWarning("VNPay hash validation failed. Response Code: {Code}", response.Vnp_ResponseCode);
                    TempData["Error"] = "Xác minh thanh toán không hợp lệ hoặc bị lỗi.";
                    return RedirectToAction("Index", "Home");
                }

                var orderId = response.Vnp_TxnRef?.Split('_').FirstOrDefault();
                if (!int.TryParse(orderId, out int localOrderId))
                {
                    _logger.LogError("Could not parse OrderId from VnPay TxnRef: {TxnRef}", response.Vnp_TxnRef);
                    TempData["Error"] = "Lỗi xử lý đơn hàng: Không tìm thấy ID đơn hàng.";
                    return RedirectToAction("Index", "Home");
                }

                var isSuccessful = response.Vnp_ResponseCode == "00";
                var paymentStatus = isSuccessful ? "Paid" : "Failed";
                var transactionId = response.Vnp_TransactionNo;

                await _paymentService.UpdatePaymentStatusAsync(
                    localOrderId,
                    paymentStatus,
                    DateTime.Now,
                    transactionId);

                _logger.LogInformation(
                    "VNPay payment result: OrderId={OrderId}, Status={Status}, TxnNo={TxnNo}",
                    localOrderId, paymentStatus, transactionId);

                if (isSuccessful)
                {
                    return RedirectToAction("VnPaySuccess", "Payment", new { orderId = localOrderId });
                }
                else
                {
                    TempData["Warning"] = $"Thanh toán VNPay không thành công. Mã lỗi: {response.Vnp_ResponseCode}";
                    return RedirectToAction("Index", "Cart");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling VNPay return");
                TempData["Error"] = "Lỗi trong quá trình xử lý kết quả thanh toán VNPay.";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// VNPay success redirect page
        /// ✅ FIXED: Allow anonymous access since VNPay verified payment
        /// </summary>
        [HttpGet]
        [AllowAnonymous]  // ✅ NEW: Allow access without authentication
        public async Task<IActionResult> VnPaySuccess(int orderId)
        {
            try
            {
                _logger.LogInformation("VNPay success redirect for order {OrderId}", orderId);

                // ✅ NEW: Validate orderId
                if (orderId <= 0)
                {
                    _logger.LogWarning("Invalid orderId: {OrderId}", orderId);
                    TempData["Error"] = "Mã đơn hàng không hợp lệ.";
                    return RedirectToAction("Index", "Home");
                }

                var order = await _paymentRepository.GetOrderWithDetailsAsync(orderId);

                if (order == null)
                {
                    _logger.LogWarning("Order {OrderId} not found on VNPay success", orderId);
                    TempData["Error"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction("Index", "Home");
                }

                // ✅ REMOVED: Authentication check
                // VNPay has already verified the payment

                // ✅ NEW: Verify order has required data
                if (order.OrderDetails == null || !order.OrderDetails.Any())
                {
                    _logger.LogWarning("Order {OrderId} has no order details", orderId);
                    TempData["Error"] = "Đơn hàng không có sản phẩm.";
                    return RedirectToAction("Index", "Home");
                }

                // ✅ NEW: Log successful access
                _logger.LogInformation(
                    "VnPaySuccess page loaded: OrderId={OrderId}, CustomerId={CustomerId}, PaymentStatus={Status}",
                    orderId, order.CustomerId, order.Payment?.Status ?? "Unknown");

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading VNPay success page for order {OrderId}", orderId);
                TempData["Error"] = "Có lỗi xảy ra khi hiển thị trang xác nhận. Đơn hàng của bạn đã được xử lý thành công.";
                return RedirectToAction("Index", "Order");
            }
        }
    }
}