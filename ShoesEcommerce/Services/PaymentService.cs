using Microsoft.EntityFrameworkCore;
using ShoesEcommerce.Data;
using ShoesEcommerce.Models.Orders;
using ShoesEcommerce.Models.Payments.PayPal;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.Services.Payment;
using ShoesEcommerce.Repositories.Interfaces;

namespace ShoesEcommerce.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly PayPalClient _paypalClient;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            IPaymentRepository paymentRepository,
            PayPalClient paypalClient,
            ILogger<PaymentService> logger)
        {
            _paymentRepository = paymentRepository;
            _paypalClient = paypalClient;
            _logger = logger;
        }

        public async Task<CreateOrderResponse> CreatePayPalOrderAsync(
            int orderId,
            decimal subtotal,
            decimal discountAmount,
            decimal totalAmount,
            string returnUrl,
            string cancelUrl)
        {
            try
            {
                _logger.LogInformation(
                    "Creating PayPal order for Order ID {OrderId}: Subtotal={Subtotal}, Discount={Discount}, Total={Total}",
                    orderId, subtotal, discountAmount, totalAmount);

                // Get order with items to send product details to PayPal
                var order = await _paymentRepository.GetOrderWithItemsForPaymentAsync(orderId);
                if (order == null)
                {
                    _logger.LogError("Order {OrderId} not found when creating PayPal order", orderId);
                    throw new InvalidOperationException($"Order {orderId} not found");
                }

                // Create or get invoice number
                var invoiceNumber = order.Invoice?.InvoiceNumber ?? $"INV-{orderId:D6}";
                
                // If invoice doesn't exist, create it with Draft status
                if (order.Invoice == null)
                {
                    await _paymentRepository.CreateOrUpdateInvoiceAsync(orderId, invoiceNumber, totalAmount);
                    _logger.LogInformation("Draft Invoice created for order {OrderId}: {InvoiceNumber}", orderId, invoiceNumber);
                }

                // ✅ FIX: Update invoice to Pending status when payment is initiated
                await _paymentRepository.UpdateInvoiceStatusAsync(orderId, InvoiceStatus.Pending);
                _logger.LogInformation("Invoice status updated to Pending for order {OrderId}", orderId);

                // Build items list from order details
                var items = new List<Item>();
                foreach (var detail in order.OrderDetails)
                {
                    var productName = detail.ProductVariant?.Product?.Name ?? "Sản phẩm";
                    var variant = detail.ProductVariant;
                    var variantInfo = variant != null ? $" ({variant.Color}, Size {variant.Size})" : "";
                    
                    items.Add(new Item
                    {
                        name = $"{productName}{variantInfo}",
                        quantity = detail.Quantity.ToString(),
                        description = $"Order #{orderId} - {productName}",
                        sku = variant?.Id.ToString() ?? detail.Id.ToString(),
                        category = "PHYSICAL_GOODS",
                        unit_amount = new UnitAmount
                        {
                            currency_code = "VND", // Will be converted in PayPalClient
                            value = detail.UnitPrice.ToString("F0")
                        }
                    });
                }

                _logger.LogInformation("Building PayPal order with {ItemCount} items for Order #{OrderId}", items.Count, orderId);

                // Create reference ID
                var referenceId = $"ORD-{orderId}";
                var description = $"SPORTS Vietnam - Đơn hàng #{orderId}";

                // Create PayPal order with items and invoice ID
                var paypalOrder = await _paypalClient.CreateOrderAsync(
                    subtotal,
                    discountAmount,
                    totalAmount,
                    referenceId,
                    returnUrl,
                    cancelUrl,
                    description,
                    invoiceNumber, // Pass invoice ID to PayPal
                    items);

                // ✅ FIX: Store PayPal Order ID in invoice for tracking
                await _paymentRepository.SetInvoicePayPalOrderIdAsync(orderId, paypalOrder.id);

                // Create or update payment record
                var payment = await _paymentRepository.GetByOrderIdAsync(orderId);

                if (payment == null)
                {
                    payment = new Models.Orders.Payment
                    {
                        OrderId = orderId,
                        Method = "PayPal",
                        Status = "Pending"
                    };
                    await _paymentRepository.CreateAsync(payment);
                }
                else
                {
                    payment.Method = "PayPal";
                    payment.Status = "Pending";
                    await _paymentRepository.UpdateAsync(payment);
                }

                _logger.LogInformation(
                    "PayPal order created successfully: PayPalOrderId={PayPalOrderId}, OrderId={OrderId}, InvoiceId={InvoiceId}",
                    paypalOrder.id, orderId, invoiceNumber);

                return paypalOrder;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayPal order for Order ID {OrderId}", orderId);
                
                // ✅ FIX: Rollback invoice status to Draft on failure
                try
                {
                    await _paymentRepository.UpdateInvoiceStatusAsync(orderId, InvoiceStatus.Draft);
                    _logger.LogWarning("Invoice rolled back to Draft status for order {OrderId} due to PayPal creation failure", orderId);
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "Failed to rollback invoice status for order {OrderId}", orderId);
                }
                
                throw;
            }
        }

        public async Task<CaptureOrderResponse> CapturePayPalOrderAsync(string paypalOrderId)
        {
            try
            {
                _logger.LogInformation("Capturing PayPal order: {PayPalOrderId}", paypalOrderId);

                var response = await _paypalClient.CaptureOrderAsync(paypalOrderId);

                _logger.LogInformation(
                    "PayPal order captured: {PayPalOrderId}, Status={Status}",
                    paypalOrderId, response.status);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing PayPal order {PayPalOrderId}", paypalOrderId);
                throw;
            }
        }

        public async Task<bool> UpdatePaymentStatusAsync(int orderId, string status, DateTime? paidAt = null, string? transactionId = null)
        {
            try
            {
                _logger.LogInformation(
                    "Updating payment status for order {OrderId}: Status={Status}, PaidAt={PaidAt}, TransactionId={TransactionId}",
                    orderId, status, paidAt, transactionId);

                var result = await _paymentRepository.UpdateStatusAsync(orderId, status, paidAt, transactionId);

                if (result)
                {
                    _logger.LogInformation("Payment status updated successfully for order {OrderId}", orderId);
                    
                    // If payment is successful, update invoice to Paid
                    if (status == "Paid" && paidAt.HasValue && !string.IsNullOrEmpty(transactionId))
                    {
                        await _paymentRepository.UpdateInvoiceOnPaymentAsync(orderId, transactionId, paidAt.Value);
                        await _paymentRepository.UpdateInvoiceStatusAsync(orderId, InvoiceStatus.Paid);
                        _logger.LogInformation("Invoice marked as Paid for order {OrderId}", orderId);
                    }
                    // If payment failed, update invoice to Cancelled
                    else if (status == "Failed" || status == "Cancelled")
                    {
                        await _paymentRepository.CancelInvoiceAsync(orderId, $"Payment {status}");
                        _logger.LogInformation("Invoice cancelled for order {OrderId} due to payment {Status}", orderId, status);
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to update payment status for order {OrderId}", orderId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment status for order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<bool> VerifyPayPalPaymentAsync(string paypalOrderId)
        {
            try
            {
                _logger.LogInformation("Verifying PayPal payment: {PayPalOrderId}", paypalOrderId);

                var orderDetails = await _paypalClient.GetOrderDetailsAsync(paypalOrderId);
                
                var isCompleted = orderDetails.status == "COMPLETED";

                _logger.LogInformation(
                    "PayPal payment verification: {PayPalOrderId}, Status={Status}, IsCompleted={IsCompleted}",
                    paypalOrderId, orderDetails.status, isCompleted);

                return isCompleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying PayPal payment {PayPalOrderId}", paypalOrderId);
                throw;
            }
        }

        public async Task<bool> CompletePaymentAsync(int orderId, string transactionId, DateTime paidAt)
        {
            try
            {
                _logger.LogInformation(
                    "Completing payment for order {OrderId}: TransactionId={TransactionId}, PaidAt={PaidAt}",
                    orderId, transactionId, paidAt);

                // Update payment status
                var paymentUpdated = await _paymentRepository.UpdateStatusAsync(orderId, "Paid", paidAt, transactionId);
                
                if (!paymentUpdated)
                {
                    _logger.LogWarning("Failed to update payment status for order {OrderId}", orderId);
                    return false;
                }

                // ✅ FIX: Update invoice with transaction ID and status
                var invoiceUpdated = await _paymentRepository.FinalizeInvoiceAsync(orderId, transactionId, paidAt);
                
                if (!invoiceUpdated)
                {
                    _logger.LogWarning("Failed to finalize invoice for order {OrderId}", orderId);
                    // Don't fail the whole operation if invoice update fails
                }

                _logger.LogInformation("Payment completed successfully for order {OrderId}, Invoice finalized", orderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing payment for order {OrderId}", orderId);
                throw;
            }
        }

        /// <summary>
        /// ✅ NEW: Handle payment failure with proper rollback
        /// </summary>
        public async Task<bool> HandlePaymentFailureAsync(int orderId, string reason)
        {
            try
            {
                _logger.LogWarning("Handling payment failure for order {OrderId}: {Reason}", orderId, reason);

                // Update payment status to Failed
                await _paymentRepository.UpdateStatusAsync(orderId, "Failed", null, null);

                // Cancel invoice
                await _paymentRepository.CancelInvoiceAsync(orderId, reason);

                // Update order status to PaymentFailed
                await _paymentRepository.UpdateOrderStatusAsync(orderId, "PaymentFailed");

                _logger.LogInformation("Payment failure handled for order {OrderId}, invoice cancelled", orderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling payment failure for order {OrderId}", orderId);
                return false;
            }
        }

        /// <summary>
        /// ✅ NEW: Handle user cancelled payment
        /// </summary>
        public async Task<bool> HandlePaymentCancellationAsync(int orderId)
        {
            try
            {
                _logger.LogInformation("Handling payment cancellation for order {OrderId}", orderId);

                // Update payment status to Cancelled
                await _paymentRepository.UpdateStatusAsync(orderId, "Cancelled", null, null);

                // Cancel invoice with reason
                await _paymentRepository.CancelInvoiceAsync(orderId, "Cancelled by user");

                // Update order status
                await _paymentRepository.UpdateOrderStatusAsync(orderId, "Cancelled");

                _logger.LogInformation("Payment cancellation handled for order {OrderId}", orderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling payment cancellation for order {OrderId}", orderId);
                return false;
            }
        }

        public async Task<Models.Orders.Payment?> GetPaymentByOrderIdAsync(int orderId)
        {
            try
            {
                return await _paymentRepository.GetByOrderIdAsync(orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment for order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<Models.Orders.Payment> CreatePaymentAsync(int orderId, string method, string status)
        {
            try
            {
                var payment = new Models.Orders.Payment
                {
                    OrderId = orderId,
                    Method = method,
                    Status = status
                };

                return await _paymentRepository.CreateAsync(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment for order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<Order> PrepareVnPayPaymentAsync(int orderId)
        {
            _logger.LogInformation("Preparing VNPay payment for order {OrderId}", orderId);

            var order = await _paymentRepository.GetOrderWithItemsForPaymentAsync(orderId)
                ?? throw new InvalidOperationException($"Order {orderId} not found");

            // Build invoice number and ensure invoice exists
            var invoiceNumber = order.Invoice?.InvoiceNumber ?? $"INV-{orderId:D6}";
            await _paymentRepository.CreateOrUpdateInvoiceAsync(orderId, invoiceNumber, order.TotalAmount);
            await _paymentRepository.UpdateInvoiceStatusAsync(orderId, InvoiceStatus.Pending);

            // Create or update payment record
            var payment = await _paymentRepository.GetByOrderIdAsync(orderId);
            if (payment == null)
            {
                payment = new Payment
                {
                    OrderId = orderId,
                    Method = "VNPay",
                    Status = "Pending"
                };
                await _paymentRepository.CreateAsync(payment);
            }
            else
            {
                payment.Method = "VNPay";
                payment.Status = "Pending";
                await _paymentRepository.UpdateAsync(payment);
            }

            // Move order to pending payment
            await _paymentRepository.UpdateOrderStatusAsync(orderId, "PendingPayment");

            _logger.LogInformation("VNPay payment prepared for order {OrderId}. Invoice {InvoiceNumber} set to Pending.", orderId, invoiceNumber);
            return order;
        }
    }
}
