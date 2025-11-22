using Microsoft.EntityFrameworkCore;
using ShoesEcommerce.Data;
using ShoesEcommerce.Models.Accounts;
using ShoesEcommerce.Models.Carts;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.Repositories.Interfaces;
using ShoesEcommerce.ViewModels.Account;
using System.Threading.Tasks;

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
        private readonly ICustomerRepository _customerRepository;

        public CustomerRegistrationService(
            AppDbContext context,
            ICustomerService customerService,
            ILogger<CustomerRegistrationService> logger,
            ICustomerRepository customerRepository)
        {
            _context = context;
            _customerService = customerService;
            _logger = logger;
            _customerRepository = customerRepository;
        }

        public async Task<CustomerRegistrationResult> RegisterCustomerWithCartAsync(RegisterViewModel model)
        {
            var result = new CustomerRegistrationResult();

            try
            {
                _logger.LogInformation("🚀 Starting complete customer registration for {Email}", model.Email);

                // Step 1: Check if email already exists
                var existingCustomer = await _customerRepository.GetCustomerByEmailAsync(model.Email);
                if (existingCustomer != null)
                {
                    _logger.LogWarning("⚠️ Email already exists: {Email}", model.Email);
                    result.Success = false;
                    result.ErrorMessage = "Email đã được sử dụng.";
                    return result;
                }

                // Step 2: Check if phone already exists
                if (await _customerRepository.PhoneExistsAsync(model.PhoneNumber))
                {
                    _logger.LogWarning("⚠️ Phone already exists: {Phone}", model.PhoneNumber);
                    result.Success = false;
                    result.ErrorMessage = "Số điện thoại đã được sử dụng.";
                    result.ValidationErrors["PhoneNumber"] = "Số điện thoại đã được sử dụng.";
                    return result;
                }

                // Step 3: Create Customer entity from ViewModel with proper date handling
                var customer = new Customer
                {
                    FirstName = model.FirstName.Trim(),
                    LastName = model.LastName.Trim(),
                    Email = model.Email.Trim().ToLower(),
                    PhoneNumber = model.PhoneNumber.Trim(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    DateOfBirth = DateTime.SpecifyKind(model.DateOfBirth.Date, DateTimeKind.Utc), // ✅ FIX: Ensure UTC
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _logger.LogDebug("📅 DateOfBirth processing: Input={Input}, UTC={UTC}", 
                    model.DateOfBirth, customer.DateOfBirth);

                // Step 4: Use repository method to handle transaction (creates customer, cart, and assigns role)
                var createdCustomer = await _customerRepository.RegisterCustomerWithCartAndRoleAsync(customer, "Customer");

                if (createdCustomer == null)
                {
                    throw new Exception("Repository failed to create customer");
                }

                result.Success = true;
                result.Customer = createdCustomer;

                _logger.LogInformation("🎉 Complete customer registration successful for {Email} - Customer ID: {CustomerId}, Cart ID: {CartId}, Role: Assigned", 
                    model.Email, createdCustomer.Id, createdCustomer.CartId);

                return result;
            }
            catch (Exception ex)
            {
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
                _logger.LogDebug("🔍 Checking if Customer role exists...");

                var role = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Name == "Customer" && r.UserType == UserType.Customer);

                if (role == null)
                {
                    _logger.LogInformation("➕ Creating Customer role...");
                    role = new Role
                    {
                        Name = "Customer",
                        UserType = UserType.Customer
                    };
                    _context.Roles.Add(role);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("✅ Customer role created with ID: {RoleId}", role.Id);
                }
                else
                {
                    _logger.LogDebug("✅ Customer role already exists with ID: {RoleId}", role.Id);
                }

                return role;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error ensuring Customer role exists");
                throw;
            }
        }

        public async Task<Cart> CreateCartForCustomerAsync(int customerId)
        {
            try
            {
                _logger.LogDebug("🛒 Creating cart for customer {CustomerId}...", customerId);

                var cart = new Cart
                {
                    SessionId = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CartItems = new List<CartItem>() // Explicit List initialization
                };

                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Cart created with ID: {CartId} for customer {CustomerId}", cart.Id, customerId);
                return cart;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating cart for customer {CustomerId}", customerId);
                throw;
            }
        }

        public async Task<bool> AssignDefaultCustomerRoleAsync(int customerId)
        {
            try
            {
                _logger.LogDebug("🔐 Assigning customer role to customer {CustomerId}...", customerId);

                // Ensure Customer role exists
                var customerRole = await EnsureCustomerRoleExistsAsync();

                // Check if role is already assigned
                var existingAssignment = await _context.UserRoles
                    .AnyAsync(ur => ur.CustomerId == customerId && ur.RoleId == customerRole.Id);

                if (existingAssignment)
                {
                    _logger.LogDebug("✅ Customer role already assigned to customer {CustomerId}", customerId);
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

                _logger.LogInformation("✅ Customer role (ID: {RoleId}) assigned to customer {CustomerId} successfully", 
                    customerRole.Id, customerId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error assigning customer role to customer {CustomerId}", customerId);
                throw; // Re-throw to trigger transaction rollback
            }
        }
    }
}