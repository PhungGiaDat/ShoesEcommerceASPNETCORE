using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using ShoesEcommerce.Data;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.Services;
using ShoesEcommerce.Repositories.Interfaces;
using ShoesEcommerce.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using ShoesEcommerce.Middleware;
using ShoesEcommerce.ModelBinders;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using ShoesEcommerce.Services.Payment;

var builder = WebApplication.CreateBuilder(args);

// Enhanced logging configuration
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Only add Debug logging in development
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddDebug();
}

// Log current environment for debugging
Console.WriteLine($"🔧 Current Environment: {builder.Environment.EnvironmentName}");

// Configure detailed logging for specific components
builder.Services.Configure<LoggerFilterOptions>(options =>
{
    options.MinLevel = builder.Environment.IsDevelopment() ? LogLevel.Debug : LogLevel.Information;
    options.AddFilter("Microsoft.EntityFrameworkCore", builder.Environment.IsDevelopment() ? LogLevel.Information : LogLevel.Warning);
    options.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
    options.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
});

// 🕐 Configure DateTime Culture and Localization
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("en-US"),
        new CultureInfo("vi-VN")
    };

    options.DefaultRequestCulture = new RequestCulture("en-US");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    
    options.RequestCultureProviders.Clear();
    options.RequestCultureProviders.Add(new QueryStringRequestCultureProvider());
    options.RequestCultureProviders.Add(new CookieRequestCultureProvider());
    options.RequestCultureProviders.Add(new AcceptLanguageHeaderRequestCultureProvider());
});

// ✅ Get connection string - Check multiple sources
var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING");

if (string.IsNullOrEmpty(connectionString))
{
    // Try from configuration (appsettings.json or appsettings.Development.json)
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}

// Log connection string status (masked for security)
if (!string.IsNullOrEmpty(connectionString))
{
    var maskedConnStr = connectionString.Length > 50 
        ? connectionString.Substring(0, 30) + "***" + connectionString.Substring(connectionString.Length - 20)
        : "***configured***";
    Console.WriteLine($"✅ Database connection string found: {maskedConnStr}");
}
else
{
    Console.WriteLine("❌ WARNING: Database connection string is empty or not configured!");
    Console.WriteLine("   Please set DATABASE_CONNECTION_STRING environment variable");
    Console.WriteLine("   Or configure ConnectionStrings:DefaultConnection in appsettings.Development.json");
}

// Add DbContext with the connection string
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException(
            "Database connection string not configured. " +
            "Set DATABASE_CONNECTION_STRING environment variable or configure ConnectionStrings:DefaultConnection in appsettings.json/appsettings.Development.json");
    }
    
    options.UseNpgsql(connectionString);
    
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Register repositories
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IDiscountRepository, DiscountRepository>();
builder.Services.AddScoped<IStockRepository, StockRepository>();
builder.Services.AddScoped<IStaffRepository, StaffRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IQARepository, QARepository>();
builder.Services.AddScoped<CheckoutRepository>();
builder.Services.AddScoped<ShoesEcommerce.Repositories.Interfaces.IPaymentRepository, ShoesEcommerce.Repositories.PaymentRepository>();

// Register services
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<IStaffService, StaffService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ICustomerRegistrationService, CustomerRegistrationService>();
builder.Services.AddScoped<IStaffRegistrationService, StaffRegistrationService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IDiscountService, DiscountService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<ShoesEcommerce.Services.ICommentService, ShoesEcommerce.Services.CommentService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ICheckoutService, CheckoutService>();

// Register VNPayService
builder.Services.AddScoped<IVnPayService, VnPayService>(); 

// Register PayPal HttpClient
builder.Services.AddHttpClient("PayPal", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("Accept-Language", "en_US");
});

// ✅ Register PayPal Client - Make it optional (don't crash if not configured)
builder.Services.AddSingleton<ShoesEcommerce.Services.Payment.PayPalClient>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<ShoesEcommerce.Services.Payment.PayPalClient>>();
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    
    var clientId = Environment.GetEnvironmentVariable("PAYPAL_CLIENT_ID") 
                   ?? configuration["PayPalOptions:ClientId"];
    var clientSecret = Environment.GetEnvironmentVariable("PAYPAL_CLIENT_SECRET") 
                       ?? configuration["PayPalOptions:ClientSecret"];
    var mode = Environment.GetEnvironmentVariable("PAYPAL_MODE") 
               ?? configuration["PayPalOptions:Mode"] 
               ?? "Sandbox";

    if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
    {
        logger.LogWarning("⚠️ PayPal configuration missing. PayPal payment will not be available.");
        // Return a dummy client or throw - for now we throw to maintain existing behavior
        throw new InvalidOperationException("PayPal ClientId and ClientSecret must be configured");
    }

    logger.LogInformation("✅ PayPal Client initialized in {Mode} mode", mode);
    return new ShoesEcommerce.Services.Payment.PayPalClient(clientId, clientSecret, mode, logger, httpClientFactory);
});

// Register HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Configure session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Configure authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

// Configure MVC
builder.Services.AddControllersWithViews(options =>
{
    options.ModelBinderProviders.Insert(0, new DateTimeModelBinderProvider());
})
.ConfigureApiBehaviorOptions(options =>
{
    options.SuppressModelStateInvalidFilter = false;
});

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

try
{
    logger.LogInformation("🚀 ShoesEcommerce application starting up...");
    logger.LogInformation("📍 Environment: {Environment}", app.Environment.EnvironmentName);

    // Seed database
    try
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Test connection first
        var canConnect = await context.Database.CanConnectAsync();
        logger.LogInformation("📊 Database connection test: {Status}", canConnect ? "SUCCESS" : "FAILED");
        
        if (canConnect)
        {
            await context.Database.EnsureCreatedAsync();
            await DataSeeder.SeedAllDataAsync(context);
            logger.LogInformation("✅ Database seeded successfully");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ An error occurred while connecting to or seeding the database");
    }

    // Configure HTTP pipeline
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseStatusCodePagesWithReExecute("/Error/{0}");
        app.UseHsts();
        logger.LogInformation("🔒 Production error handling configured");
    }
    else
    {
        app.UseDeveloperExceptionPage();
        logger.LogInformation("🔧 Developer exception page enabled");
    }

    app.UseSession();
    app.UseRequestLocalization();
    app.UseErrorLogging();
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();

    // Admin routes
    app.MapControllerRoute(name: "admin_product", pattern: "Admin/Product/{action=Index}/{id?}", defaults: new { controller = "AdminProduct" });
    app.MapControllerRoute(name: "admin_stock", pattern: "Admin/Stock/{action=Index}/{id?}", defaults: new { controller = "AdminStock" });
    app.MapControllerRoute(name: "admin_staff", pattern: "Admin/Staff/{action=Index}/{id?}", defaults: new { controller = "AdminStaff" });
    app.MapControllerRoute(name: "admin_order", pattern: "Admin/Order/{action=Index}/{id?}", defaults: new { controller = "AdminOrder" });
    app.MapControllerRoute(name: "admin_discount", pattern: "Admin/Discount/{action=Index}/{id?}", defaults: new { controller = "AdminDiscount" });
    app.MapControllerRoute(name: "admin_data", pattern: "Admin/Data/{action=Index}/{id?}", defaults: new { controller = "Data" });
    app.MapControllerRoute(name: "admin_default", pattern: "Admin/{action=Index}/{id?}", defaults: new { controller = "Admin" });

    // SEO routes
    app.MapControllerRoute(name: "product_detail_seo", pattern: "san-pham/{slug}", defaults: new { controller = "Product", action = "Details" });
    app.MapControllerRoute(name: "product_list_seo", pattern: "san-pham", defaults: new { controller = "Product", action = "Index" });
    app.MapControllerRoute(name: "discounted_products_seo", pattern: "khuyen-mai", defaults: new { controller = "Product", action = "DiscountedProducts" });
    app.MapControllerRoute(name: "cart_seo", pattern: "gio-hang", defaults: new { controller = "Cart", action = "Index" });
    app.MapControllerRoute(name: "checkout_seo", pattern: "thanh-toan", defaults: new { controller = "Checkout", action = "Index" });
    app.MapControllerRoute(name: "login_seo", pattern: "dang-nhap", defaults: new { controller = "Account", action = "Login" });
    app.MapControllerRoute(name: "register_seo", pattern: "dang-ky", defaults: new { controller = "Account", action = "Register" });
    app.MapControllerRoute(name: "orders_seo", pattern: "don-hang", defaults: new { controller = "Order", action = "Index" });
    app.MapControllerRoute(name: "order_detail_seo", pattern: "don-hang/{id}", defaults: new { controller = "Order", action = "Details" });

    // Default route
    app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
    app.MapControllerRoute(name: "error", pattern: "Error/{statusCode?}", defaults: new { controller = "Error", action = "HandleErrorCode" });

    // Health check endpoint
    app.MapGet("/health", () => Results.Ok(new { 
        status = "healthy", 
        timestamp = DateTime.UtcNow,
        environment = app.Environment.EnvironmentName,
        version = "1.0.0"
    }));

    logger.LogInformation("✅ ShoesEcommerce application started successfully");
    app.Run();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "💥 Application terminated unexpectedly during startup");
    throw;
}
