using Microsoft.EntityFrameworkCore;
using ShoesEcommerce.Models.Accounts;
using ShoesEcommerce.Models.Products;
using DepartmentEntity = ShoesEcommerce.Models.Departments.Department;

namespace ShoesEcommerce.Data
{
    public static class DataSeeder
    {
        public static async Task SeedDepartmentsAsync(AppDbContext context)
        {
            // Check if departments already exist
            if (await context.Departments.AnyAsync())
                return;

            var departments = new List<DepartmentEntity>
            {
                new DepartmentEntity { Name = "Administration" },
                new DepartmentEntity { Name = "Sales" },
                new DepartmentEntity { Name = "Marketing" },
                new DepartmentEntity { Name = "Customer Service" },
                new DepartmentEntity { Name = "Warehouse" }
            };

            await context.Departments.AddRangeAsync(departments);
            await context.SaveChangesAsync();
        }

        public static async Task SeedRolesAsync(AppDbContext context)
        {
            // Check if roles already exist
            if (await context.Roles.AnyAsync())
                return;

            var roles = new List<Role>
            {
                new Role { Name = "Admin", UserType = UserType.Staff },
                new Role { Name = "Staff", UserType = UserType.Staff },
                new Role { Name = "Manager", UserType = UserType.Staff },
                new Role { Name = "Customer", UserType = UserType.Customer }
            };

            await context.Roles.AddRangeAsync(roles);
            await context.SaveChangesAsync();
        }

        public static async Task SeedStaffAsync(AppDbContext context)
        {
            // Check if staff already exist
            if (await context.Staffs.AnyAsync())
                return;

            // Get departments and roles
            var adminDept = await context.Departments.FirstOrDefaultAsync(d => d.Name == "Administration");
            var salesDept = await context.Departments.FirstOrDefaultAsync(d => d.Name == "Sales");
            
            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin" && r.UserType == UserType.Staff);
            var staffRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Staff" && r.UserType == UserType.Staff);

            if (adminDept == null || salesDept == null || adminRole == null || staffRole == null)
                return;

            var staffMembers = new List<Staff>
            {
                new Staff
                {
                    Email = "admin@shoesstore.vn",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                    FirstName = "System",
                    LastName = "Administrator",
                    PhoneNumber = "0901000001",
                    DepartmentId = adminDept.Id
                },
                new Staff
                {
                    Email = "manager@shoesstore.vn", 
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager123!"),
                    FirstName = "Store",
                    LastName = "Manager",
                    PhoneNumber = "0901000002",
                    DepartmentId = salesDept.Id
                },
                new Staff
                {
                    Email = "staff@shoesstore.vn",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff123!"),
                    FirstName = "Sales",
                    LastName = "Staff",
                    PhoneNumber = "0901000003",
                    DepartmentId = salesDept.Id
                }
            };

            await context.Staffs.AddRangeAsync(staffMembers);
            await context.SaveChangesAsync();

            // Assign roles to staff
            var admin = await context.Staffs.FirstOrDefaultAsync(s => s.Email == "admin@shoesstore.vn");
            var manager = await context.Staffs.FirstOrDefaultAsync(s => s.Email == "manager@shoesstore.vn");
            var staff = await context.Staffs.FirstOrDefaultAsync(s => s.Email == "staff@shoesstore.vn");

            if (admin != null && manager != null && staff != null)
            {
                var userRoles = new List<UserRole>
                {
                    new UserRole { StaffId = admin.Id, RoleId = adminRole.Id },
                    new UserRole { StaffId = manager.Id, RoleId = staffRole.Id },
                    new UserRole { StaffId = staff.Id, RoleId = staffRole.Id }
                };

                await context.UserRoles.AddRangeAsync(userRoles);
                await context.SaveChangesAsync();
            }
        }

        public static async Task SeedSuppliersAsync(AppDbContext context)
        {
            // Check if suppliers already exist
            if (await context.Suppliers.AnyAsync())
                return;

            var suppliers = new List<Supplier>
            {
                new Supplier
                {
                    Name = "Nike Vietnam",
                    ContactInfo = "nike@vietnam.com - 0901234567"
                },
                new Supplier
                {
                    Name = "Adidas Distribution",
                    ContactInfo = "adidas@supplier.com - 0902345678"
                },
                new Supplier
                {
                    Name = "Converse Official",
                    ContactInfo = "converse@official.vn - 0903456789"
                },
                new Supplier
                {
                    Name = "Vans Authentic",
                    ContactInfo = "vans@authentic.com - 0904567890"
                },
                new Supplier
                {
                    Name = "Local Shoe Manufacturer",
                    ContactInfo = "local@shoes.vn - 0905678901"
                }
            };

            await context.Suppliers.AddRangeAsync(suppliers);
            await context.SaveChangesAsync();
        }

        public static async Task SeedAllDataAsync(AppDbContext context)
        {
            await SeedDepartmentsAsync(context);
            await SeedRolesAsync(context);
            await SeedStaffAsync(context);
            await SeedSuppliersAsync(context);
        }
    }
}