using ShoesEcommerce.Models.Accounts;
using ShoesEcommerce.ViewModels.Account;

namespace ShoesEcommerce.Services.Interfaces
{
    /// <summary>
    /// Service interface for handling complete customer registration process
    /// including cart creation and role assignment
    /// </summary>
    public interface ICustomerRegistrationService
    {
        /// <summary>
        /// Complete customer registration process including:
        /// - Creating customer account
        /// - Creating empty cart for customer
        /// - Assigning default "Customer" role
        /// </summary>
        /// <param name="model">Registration view model</param>
        /// <returns>Registration result with customer and cart information</returns>
        Task<CustomerRegistrationResult> RegisterCustomerWithCartAsync(RegisterViewModel model);

        /// <summary>
        /// Ensures the default "Customer" role exists in the system
        /// </summary>
        /// <returns>The Customer role</returns>
        Task<Role> EnsureCustomerRoleExistsAsync();

        /// <summary>
        /// Creates an empty cart for a customer
        /// </summary>
        /// <param name="customerId">Customer ID</param>
        /// <returns>Created cart</returns>
        Task<ShoesEcommerce.Models.Carts.Cart> CreateCartForCustomerAsync(int customerId);

        /// <summary>
        /// Assigns the default customer role to a customer
        /// </summary>
        /// <param name="customerId">Customer ID</param>
        /// <returns>True if successful</returns>
        Task<bool> AssignDefaultCustomerRoleAsync(int customerId);
        
    }
}