﻿using Microsoft.EntityFrameworkCore;
using ShoesEcommerce.Models.Accounts;
using ShoesEcommerce.Models.Carts;
using DepartmentEntity = ShoesEcommerce.Models.Departments.Department;
using ShoesEcommerce.Models.Interactions;
using ShoesEcommerce.Models.Orders;
using ShoesEcommerce.Models.Products;
using ShoesEcommerce.Models.Stocks;

namespace ShoesEcommerce.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // ===== DbSet =====

        // Accounts
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Staff> Staffs { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }

        // Departments
        public DbSet<DepartmentEntity> Departments { get; set; }


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

        // Carts
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }

        // Stocks
        public DbSet<Stock> Stocks { get; set; }
        public DbSet<StockEntry> StockEntries { get; set; }
        public DbSet<StockTransaction> StockTransactions { get; set; }

        // Interactions
        public DbSet<Comment> Comments { get; set; }
        public DbSet<QA> QAs { get; set; }
        public DbSet<Topic> Topics { get; set; }
        public DbSet<Favorite> Favorites { get; set; }

        // ===== Fluent API =====

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureAccounts(modelBuilder);
            ConfigureOrders(modelBuilder);
            ConfigureProducts(modelBuilder);
            ConfigureCarts(modelBuilder);
            ConfigureStocks(modelBuilder);
            ConfigureInteractions(modelBuilder);
        }

        // === Fluent API Configurations ===

        private void ConfigureAccounts(ModelBuilder modelBuilder)
        {

            // =========================
            // 🔐 Customer (User từ Firebase)
            // =========================
            modelBuilder.Entity<Customer>()
                .Property(c => c.FirebaseUid)
                .HasColumnName("FirebaseUid")
                .IsRequired();

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.FirebaseUid)
                .IsUnique(); // Đảm bảo mỗi Customer có FirebaseUid duy nhất

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany()
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Staff)
                .WithMany()
                .HasForeignKey(ur => ur.StaffId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Customer)
                .WithMany()
                .HasForeignKey(ur => ur.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================
            // 🧩 Role - Permission (N-N)
            // =========================
            modelBuilder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionId });

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Restrict);

            // =========================
            // 🏢 Department - Staff (1-N)
            // =========================
            modelBuilder.Entity<Staff>()
                .HasOne(s => s.Department)
                .WithMany(d => d.Staffs)
                .HasForeignKey(s => s.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        }


        private void ConfigureOrders(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.ShippingAddress)
                .WithMany(sa => sa.Orders)
                .HasForeignKey(o => o.ShippingAddressId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Payment)
                .WithOne(p => p.Order)
                .HasForeignKey<Payment>(p => p.OrderId);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Invoice)
                .WithOne(i => i.Order)
                .HasForeignKey<Invoice>(i => i.OrderId);

            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId);

            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.ProductVariant)
                .WithMany(pv => pv.OrderDetails)
                .HasForeignKey(od => od.ProductVariantId);

            // Configure ShippingAddress-Customer relationship
            modelBuilder.Entity<ShippingAddress>()
                .HasOne(sa => sa.Customer)
                .WithMany(c => c.ShippingAddresses)
                .HasForeignKey(sa => sa.CustomerId);
        }

        private void ConfigureProducts(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Brand)
                .WithMany(b => b.Products)
                .HasForeignKey(p => p.BrandId);

            modelBuilder.Entity<ProductVariant>()
                .HasOne(pv => pv.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(pv => pv.ProductId);
        }

        private void ConfigureCarts(ModelBuilder modelBuilder)
        {
            // Configure Cart-Customer relationship
            modelBuilder.Entity<Cart>()
                .HasOne(c => c.Customer)
                .WithOne(c => c.Cart)
                .HasForeignKey<Customer>(c => c.CartId);

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Cart)
                .WithMany(c => c.CartItems)
                .HasForeignKey(ci => ci.CartId);

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.ProductVariant)
                .WithMany(pv => pv.CartItems)
                .HasForeignKey(ci => ci.ProductVarientId);
        }

        private void ConfigureStocks(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Stock>()
                .HasOne(s => s.ProductVariant)
                .WithMany(pv => pv.Stocks)
                .HasForeignKey(s => s.ProductVariantId);

            modelBuilder.Entity<StockEntry>()
                .HasOne(se => se.ProductVariant)
                .WithMany(pv => pv.StockEntries)
                .HasForeignKey(se => se.ProductVariantId);

            modelBuilder.Entity<StockEntry>()
                .HasOne(se => se.Supplier)
                .WithMany(s => s.StockEntries)
                .HasForeignKey(se => se.SupplierId);

            modelBuilder.Entity<StockTransaction>()
                .HasOne(st => st.ProductVariant)
                .WithMany(pv => pv.StockTransactions)
                .HasForeignKey(st => st.ProductVariantId);
        }

        private void ConfigureInteractions(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Customer)
                .WithMany(cus => cus.Comments)
                .HasForeignKey(c => c.CustomerId);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Product)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.ProductId);

            modelBuilder.Entity<Favorite>()
                .HasOne(f => f.Customer)
                .WithMany(c => c.Favorites)
                .HasForeignKey(f => f.CustomerId);

            modelBuilder.Entity<Favorite>()
                .HasOne(f => f.Product)
                .WithMany(p => p.Favorites)
                .HasForeignKey(f => f.ProductId);

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

            modelBuilder.Entity<QA>()
                .HasOne(q => q.Product)
                .WithMany(p => p.QAs)
                .HasForeignKey(q => q.ProductId);

            modelBuilder.Entity<QA>()
                .HasOne(q => q.Topic)
                .WithMany(t => t.QAs)
                .HasForeignKey(q => q.TopicId);
        }
    }
}
