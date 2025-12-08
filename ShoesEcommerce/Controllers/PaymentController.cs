using Microsoft.AspNetCore.Mvc;
using ShoesEcommerce.Models.Payments.PayPal;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.Repositories.Interfaces;
using System.Security.Claims;

namespace ShoesEcommerce.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly IPaymentRepository _paymentRepository;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IPaymentService paymentService,
            IPaymentRepository paymentRepository,
            ILogger<PaymentController> _logger)
        {
            _paymentService = paymentService;
            _paymentRepository = paymentRepository;
            this._logger = _logger;
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
                    return BadRequest(new { error = "??n hàng không t?n t?i" });
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
                        return BadRequest(new { error = "B?n không có quy?n truy c?p ??n hàng này" });
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
        public async Task<IActionResult> PayPalSuccess(int orderId, string? token)
        {
            try
            {
                _logger.LogInformation(
                    "PayPal success redirect: OrderId={OrderId}, Token={Token}",
                    orderId, token);

                // ? Get order via repository
                var order = await _paymentRepository.GetOrderWithDetailsAsync(orderId);

                if (order == null)
                {
                    _logger.LogWarning("Order {OrderId} not found on PayPal success", orderId);
                    TempData["Error"] = "Không tìm th?y ??n hàng.";
                    return RedirectToAction("Index", "Home");
                }

                // ? Verify customer access
                var customerId = GetCurrentCustomerId();
                if (customerId != 0 && order.CustomerId != customerId)
                {
                    _logger.LogWarning(
                        "Unauthorized access to order {OrderId} success page by customer {CustomerId}",
                        orderId, customerId);
                    TempData["Error"] = "B?n không có quy?n truy c?p ??n hàng này.";
                    return RedirectToAction("Index", "Home");
                }

                ViewBag.PayPalToken = token;
                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading PayPal success page for order {OrderId}", orderId);
                TempData["Error"] = "Có l?i x?y ra. Vui lòng ki?m tra l?i ??n hàng c?a b?n.";
                return RedirectToAction("Index", "Home");
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
                    TempData["Error"] = "Không tìm th?y ??n hàng.";
                    return RedirectToAction("Index", "Home");
                }

                ViewBag.OrderId = orderId;
                ViewBag.OrderTotal = order.TotalAmount;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling PayPal cancellation for order {OrderId}", orderId);
                TempData["Warning"] = "Thanh toán ?ã b? h?y.";
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
                    return NotFound(new { error = "Không tìm th?y thông tin thanh toán" });
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
    }

    /// <summary>
    /// Request model for creating PayPal order
    /// </summary>
    public class CreatePayPalOrderRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("orderId")]
        public int OrderId { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("subtotal")]
        public decimal Subtotal { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("discountAmount")]
        public decimal DiscountAmount { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("totalAmount")]
        public decimal TotalAmount { get; set; }
    }
}
