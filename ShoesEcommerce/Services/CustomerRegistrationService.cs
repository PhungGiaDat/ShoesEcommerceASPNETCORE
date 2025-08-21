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
                _logger.LogInformation("?? Starting complete customer registration with cart for {Email}", model.Email);

                // Step 1: Register the customer using existing customer service
                _logger.LogDebug("Step 1: Creating customer account...");
                var customerResult = await _customerService.RegisterCustomerAsync(model);
                
                if (!customerResult.Success)
                {
                    _logger.LogWarning("? Customer registration failed: {Error}", customerResult.ErrorMessage);
                    result.Success = false;
                    result.ErrorMessage = customerResult.ErrorMessage;
                    result.ValidationErrors = customerResult.ValidationErrors;
                    return result;
                }

                var customer = customerResult.Customer!;
                _logger.LogInformation("? Customer created successfully with ID: {CustomerId}", customer.Id);

                // Step 2: Create a cart for the customer
                _logger.LogDebug("Step 2: Creating cart for customer...");
                var cart = await CreateCartForCustomerAsync(customer.Id);
                _logger.LogInformation("? Cart created successfully with ID: {CartId}", cart.Id);

                // Step 3: Link the cart to the customer
                _logger.LogDebug("Step 3: Linking cart to customer...");
                customer.CartId = cart.Id;
                _context.Entry(customer).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                _logger.LogDebug("? Customer-Cart relationship established");

                // Step 4: Ensure customer role exists and assign it
                _logger.LogDebug("Step 4: Assigning customer role...");
                var roleAssigned = await AssignDefaultCustomerRoleAsync(customer.Id);
                if (roleAssigned)
                {
                    _logger.LogDebug("? Customer role assigned successfully");
                }
                else
                {
                    _logger.LogWarning("?? Failed to assign customer role, but continuing...");
                }

                // Commit transaction
                await transaction.CommitAsync();

                // Step 5: Reload customer with all relationships
                var completeCustomer = await _context.Customers
                    .Include(c => c.Cart)
                    .Include(c => c.Roles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(c => c.Id == customer.Id);

                result.Success = true;
                result.Customer = completeCustomer;
                
                _logger.LogInformation("?? Complete customer registration successful: {Email} (CustomerID: {CustomerId}, CartID: {CartId})", 
                    model.Email, customer.Id, cart.Id);

                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "?? Error in complete customer registration for {Email}", model.Email);
                
                result.Success = false;
                result.ErrorMessage = "Có l?i x?y ra trong quá trình ??ng ký. Vui lòng th? l?i sau.";
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