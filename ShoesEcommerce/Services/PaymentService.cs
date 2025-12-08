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

                // Verify order exists
                if (!await _paymentRepository.OrderExistsAsync(orderId))
                {
                    _logger.LogError("Order {OrderId} not found when creating PayPal order", orderId);
                    throw new InvalidOperationException($"Order {orderId} not found");
                }

                // Create reference ID
                var referenceId = $"ORD-{orderId}-{DateTime.Now.Ticks}";
                var description = $"ShoesEcommerce Order #{orderId}";

                // Create PayPal order
                var paypalOrder = await _paypalClient.CreateOrderAsync(
                    subtotal,
                    discountAmount,
                    totalAmount,
                    referenceId,
                    returnUrl,
                    cancelUrl,
                    description);

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
                    "PayPal order created successfully: PayPalOrderId={PayPalOrderId}, OrderId={OrderId}",
                    paypalOrder.id, orderId);

                return paypalOrder;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayPal order for Order ID {OrderId}", orderId);
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
    }
}
