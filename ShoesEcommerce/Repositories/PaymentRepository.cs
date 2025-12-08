using Microsoft.EntityFrameworkCore;
using ShoesEcommerce.Data;
using ShoesEcommerce.Models.Orders;
using ShoesEcommerce.Repositories.Interfaces;

namespace ShoesEcommerce.Repositories
{
    /// <summary>
    /// Repository for Payment entity operations
    /// </summary>
    public class PaymentRepository : IPaymentRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PaymentRepository> _logger;

        public PaymentRepository(AppDbContext context, ILogger<PaymentRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Payment?> GetByOrderIdAsync(int orderId)
        {
            try
            {
                return await _context.Payments
                    .Include(p => p.Order)
                    .FirstOrDefaultAsync(p => p.OrderId == orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment for order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<Payment?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.Payments
                    .Include(p => p.Order)
                    .FirstOrDefaultAsync(p => p.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment by id {Id}", id);
                throw;
            }
        }

        public async Task<Payment> CreateAsync(Payment payment)
        {
            try
            {
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();
                return payment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment for order {OrderId}", payment.OrderId);
                throw;
            }
        }

        public async Task<bool> UpdateAsync(Payment payment)
        {
            try
            {
                _context.Payments.Update(payment);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment {PaymentId}", payment.Id);
                throw;
            }
        }

        public async Task<bool> UpdateStatusAsync(int orderId, string status, DateTime? paidAt = null, string? transactionId = null)
        {
            try
            {
                var payment = await GetByOrderIdAsync(orderId);
                if (payment == null)
                {
                    _logger.LogWarning("Payment not found for order {OrderId}", orderId);
                    return false;
                }

                payment.Status = status;
                if (paidAt.HasValue)
                {
                    payment.PaidAt = paidAt.Value;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment status for order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<Order?> GetOrderWithDetailsAsync(int orderId)
        {
            try
            {
                return await _context.Orders
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.ProductVariant)
                            .ThenInclude(pv => pv!.Product)
                    .Include(o => o.Payment)
                    .Include(o => o.ShippingAddress)
                    .FirstOrDefaultAsync(o => o.Id == orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order with details {OrderId}", orderId);
                throw;
            }
        }

        public async Task<bool> OrderExistsAsync(int orderId)
        {
            try
            {
                return await _context.Orders.AnyAsync(o => o.Id == orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if order exists {OrderId}", orderId);
                throw;
            }
        }

        public async Task<bool> OrderBelongsToCustomerAsync(int orderId, int customerId)
        {
            try
            {
                return await _context.Orders
                    .AnyAsync(o => o.Id == orderId && o.CustomerId == customerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking order ownership {OrderId}, {CustomerId}", orderId, customerId);
                throw;
            }
        }
    }
}
