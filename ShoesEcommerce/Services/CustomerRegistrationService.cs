using Microsoft.EntityFrameworkCore;
using ShoesEcommerce.Data;
using ShoesEcommerce.Models.Accounts;
using ShoesEcommerce.Models.Carts;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.ViewModels.Account;

namespace ShoesEcommerce.Services
{
    /// <summary>
    /// Service for handling complete customer registration process
    /// including cart creation and role assignment
    /// </summary>
    public class CustomerRegistrationService : ICustomerRegistrationService
    {
        private readonly AppDbContext _context;
        private readonly ICustomerService _customerService;
        private readonly ILogger<CustomerRegistrationService> _logger;

        public CustomerRegistrationService(
            AppDbContext context,
            ICustomerService customerService,
            ILogger<CustomerRegistrationService> logger)
        {
            _context = context;
            _customerService = customerService;
            _logger = logger;
        }

        public async Task<CustomerRegistrationResult> RegisterCustomerWithCartAsync(RegisterViewModel model)
        {
            var result = new CustomerRegistrationResult();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("🚀 Starting complete customer registration for {Email}", model.Email);

                // Step 1: Create Customer from ViewModel
                var customer = new Customer
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    DateOfBirth = model.DateOfBirth,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    CreatedAt = DateTime.UtcNow,
                };

                // Step 2: Create Cart for Customer
                var cart = new Cart
                {
                    SessionId = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Step 3: Link Cart to Customer in memory
                customer.Cart = cart;

                // Step 4: Add Customer to DbContext
                _context.Customers.Add(customer);

                // Step 5: Save all changes in one go
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Customer (ID: {CustomerId}) and Cart (ID: {CartId}) created and linked successfully.",
                    customer.Id, cart.Id);

                // Step 6: Assign default role
                await AssignDefaultCustomerRoleAsync(customer.Id);

                await transaction.CommitAsync();

                result.Success = true;
                result.Customer = customer;

                _logger.LogInformation("🎉 Complete customer registration successful for {Email}", model.Email);

                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "❌ Error in complete customer registration for {Email}", model.Email);

                result.Success = false;
                result.ErrorMessage = "Có lỗi xảy ra trong quá trình đăng ký. Vui lòng thử lại sau.";
                return result;
            }
        }

        public async Task<Role> EnsureCustomerRoleExistsAsync()
        {
            try
            {
                _logger.LogDebug("?? Checking if Customer role exists...");

                var role = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Name == "Customer" && r.UserType == UserType.Customer);

                if (role == null)
                {
                    _logger.LogInformation("??? Creating Customer role...");
                    role = new Role
                    {
                        Name = "Customer",
                        UserType = UserType.Customer
                    };
                    _context.Roles.Add(role);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("? Customer role created with ID: {RoleId}", role.Id);
                }
                else
                {
                    _logger.LogDebug("? Customer role already exists with ID: {RoleId}", role.Id);
                }

                return role;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "?? Error ensuring Customer role exists");
                throw;
            }
        }

        public async Task<Cart> CreateCartForCustomerAsync(int customerId)
        {
            try
            {
                _logger.LogDebug("?? Creating cart for customer {CustomerId}...", customerId);

                var cart = new Cart
                {
                    SessionId = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CartItems = new List<CartItem>() // Explicit List initialization
                };

                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();

                _logger.LogDebug("? Cart created with ID: {CartId} for customer {CustomerId}", cart.Id, customerId);
                return cart;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Error creating cart for customer {CustomerId}", customerId);
                throw;
            }
        }

        public async Task<bool> AssignDefaultCustomerRoleAsync(int customerId)
        {
            try
            {
                _logger.LogDebug("?? Assigning customer role to customer {CustomerId}...", customerId);

                // Ensure Customer role exists
                var customerRole = await EnsureCustomerRoleExistsAsync();

                // Check if role is already assigned
                var existingAssignment = await _context.UserRoles
                    .AnyAsync(ur => ur.CustomerId == customerId && ur.RoleId == customerRole.Id);

                if (existingAssignment)
                {
                    _logger.LogDebug("? Customer role already assigned to customer {CustomerId}", customerId);
                    return true;
                }

                // Assign the role
                var userRole = new UserRole
                {
                    CustomerId = customerId,
                    RoleId = customerRole.Id
                };

                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();

                _logger.LogDebug("? Customer role assigned to customer {CustomerId} successfully", customerId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "?? Error assigning customer role to customer {CustomerId}", customerId);
                return false;
            }
        }
    }
}