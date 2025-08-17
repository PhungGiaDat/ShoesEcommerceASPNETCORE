using Microsoft.EntityFrameworkCore;
using ShoesEcommerce.Models.Products;

namespace ShoesEcommerce.Data
{
    public static class DataSeeder
    {
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
            await SeedSuppliersAsync(context);
        }
    }
}