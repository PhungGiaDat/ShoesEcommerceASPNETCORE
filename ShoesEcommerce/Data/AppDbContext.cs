using Microsoft.EntityFrameworkCore;
using ShoesEcommerce.Models.Accounts;
using ShoesEcommerce.Models.Products;
using ShoesEcommerce.Models.Orders;
using ShoesEcommerce.Models.Carts;
using ShoesEcommerce.Models.Stocks;
using ShoesEcommerce.Models.Interactions;
using DepartmentAccounts = ShoesEcommerce.Models.Accounts.Department;
using DepartmentDepartments = ShoesEcommerce.Models.Departments.Department;

namespace ShoesEcommerce.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Accounts
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Staff> Staffs { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RoleStaff> RoleStaffs { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }

    // Departments
    public DbSet<DepartmentDepartments> Departments { get; set; }

    // Products
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductVariant> ProductVariants { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Brand> Brands { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }

    // Orders
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderDetail> OrderDetails { get; set; }
    public DbSet<ShippingAddress> ShippingAddresses { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Invoice> Invoices { get; set; }

    // Cart
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }

    // Stock
    public DbSet<Stock> Stocks { get; set; }
    public DbSet<StockEntry> StockEntries { get; set; }
    public DbSet<StockTransaction> StockTransactions { get; set; }

    // Interactions
    public DbSet<Comment> Comments { get; set; }
    public DbSet<QA> QAs { get; set; }
    public DbSet<Topic> Topics { get; set; }
    public DbSet<Favorite> Favorites { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // N-N: Role_Staff
        modelBuilder.Entity<RoleStaff>()
            .HasKey(rs => new { rs.StaffId, rs.RoleId });

        modelBuilder.Entity<RoleStaff>()
            .HasOne(rs => rs.Staff)
            .WithMany(s => s.RoleStaffs)
            .HasForeignKey(rs => rs.StaffId);

        modelBuilder.Entity<RoleStaff>()
            .HasOne(rs => rs.Role)
            .WithMany(r => r.RoleStaffs)
            .HasForeignKey(rs => rs.RoleId);

        // N-N: Role_Permission
        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId);

        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId);

        // Optional: QA - Customer/Staff
        modelBuilder.Entity<QA>()
            .HasOne(q => q.Customer)
            .WithMany(c => c.QAs)
            .HasForeignKey(q => q.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<QA>()
            .HasOne(q => q.Staff)
            .WithMany(s => s.QAs)
            .HasForeignKey(q => q.StaffId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
