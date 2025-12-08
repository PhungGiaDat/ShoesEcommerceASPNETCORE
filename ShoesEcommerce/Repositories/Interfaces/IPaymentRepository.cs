using ShoesEcommerce.Models.Orders;

namespace ShoesEcommerce.Repositories.Interfaces
{
    /// <summary>
    /// Interface for Payment repository operations
    /// </summary>
    public interface IPaymentRepository
    {
        /// <summary>
        /// Get payment by order ID
        /// </summary>
        Task<Payment?> GetByOrderIdAsync(int orderId);

        /// <summary>
        /// Get payment by payment ID
        /// </summary>
        Task<Payment?> GetByIdAsync(int id);

        /// <summary>
        /// Create a new payment record
        /// </summary>
        Task<Payment> CreateAsync(Payment payment);

        /// <summary>
        /// Update an existing payment
        /// </summary>
        Task<bool> UpdateAsync(Payment payment);

        /// <summary>
        /// Update payment status
        /// </summary>
        Task<bool> UpdateStatusAsync(int orderId, string status, DateTime? paidAt = null, string? transactionId = null);

        /// <summary>
        /// Get order with all details (for success/cancel pages)
        /// </summary>
        Task<Order?> GetOrderWithDetailsAsync(int orderId);

        /// <summary>
        /// Check if order exists
        /// </summary>
        Task<bool> OrderExistsAsync(int orderId);

        /// <summary>
        /// Check if order belongs to customer
        /// </summary>
        Task<bool> OrderBelongsToCustomerAsync(int orderId, int customerId);
    }
}
