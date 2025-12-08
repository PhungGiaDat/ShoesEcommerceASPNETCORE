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

var builder = WebApplication.CreateBuilder(args);

// Enhanced logging configuration
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add file logging in production
if (!builder.Environment.IsDevelopment())
{
    builder.Logging.AddEventLog();
    
    // Add file logging (requires additional NuGet package like Serilog or NLog)
    // builder.Host.UseSerilog((context, configuration) => {
    //     configuration.WriteTo.File("logs/shoescommerce-.txt", rollingInterval: RollingInterval.Day);
    // });
}

// Configure detailed logging for specific components
builder.Services.Configure<LoggerFilterOptions>(options =>
{
    // Set minimum log levels for different categories
    options.MinLevel = builder.Environment.IsDevelopment() ? LogLevel.Debug : LogLevel.Information;
    
    // Reduce noise from Entity Framework in production
    options.AddFilter("Microsoft.EntityFrameworkCore", builder.Environment.IsDevelopment() ? LogLevel.Information : LogLevel.Warning);
    options.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
    options.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
});

// 🕐 ADD: Configure DateTime Culture and Localization for proper date handling
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("en-US"), // Primary culture for consistent date parsing
        new CultureInfo("vi-VN")  // Vietnamese culture for display
    };

    options.DefaultRequestCulture = new RequestCulture("en-US");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    
    // Set date format providers to handle HTML5 date inputs consistently
    options.RequestCultureProviders.Clear();
    options.RequestCultureProviders.Add(new QueryStringRequestCultureProvider());
    options.RequestCultureProviders.Add(new CookieRequestCultureProvider());
    options.RequestCultureProviders.Add(new AcceptLanguageHeaderRequestCultureProvider());
});

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    
    // Enable detailed errors in development
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
builder.Services.AddScoped<CheckoutRepository>(); // ✅ NEW: Checkout repository
builder.Services.AddScoped<ShoesEcommerce.Repositories.Interfaces.IPaymentRepository, ShoesEcommerce.Repositories.PaymentRepository>(); // ✅ NEW: Payment repository

// Register services
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<IStaffService, StaffService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ICustomerRegistrationService, CustomerRegistrationService>();
builder.Services.AddScoped<IStaffRegistrationService, StaffRegistrationService>(); // ✅ NEW: Staff registration service
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IDiscountService, DiscountService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<ShoesEcommerce.Services.ICommentService, ShoesEcommerce.Services.CommentService>();
builder.Services.AddScoped<IPaymentService, PaymentService>(); // ✅ NEW: Payment service
builder.Services.AddScoped<ICheckoutService, CheckoutService>(); // ✅ NEW: Checkout service

// ✅ NEW: Register PayPal HttpClient
builder.Services.AddHttpClient("PayPal", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("Accept-Language", "en_US");
});

// ✅ NEW: Register PayPal Client as Singleton
builder.Services.AddSingleton<ShoesEcommerce.Services.Payment.PayPalClient>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<ShoesEcommerce.Services.Payment.PayPalClient>>();
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    
    var clientId = configuration["PayPalOptions:ClientId"];
    var clientSecret = configuration["PayPalOptions:ClientSecret"];
    var mode = configuration["PayPalOptions:Mode"] ?? "Sandbox";

    if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
    {
        logger.LogWarning("PayPal configuration missing. PayPal payment will not be available.");
        throw new InvalidOperationException("PayPal ClientId and ClientSecret must be configured in appsettings.json");
    }

    logger.LogInformation("PayPal Client initialized in {Mode} mode", mode);
    return new ShoesEcommerce.Services.Payment.PayPalClient(clientId, clientSecret, mode, logger, httpClientFactory);
});

// Register HttpContextAccessor for services
builder.Services.AddHttpContextAccessor();

// Configure session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Configure authentication - KEEP IT SIMPLE
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

// 🕐 ADD: Configure MVC with custom DateTime model binding
builder.Services.AddControllersWithViews(options =>
{
    // Add custom DateTime model binder for proper HTML5 date input handling
    options.ModelBinderProviders.Insert(0, new DateTimeModelBinderProvider());
    
    // Add global exception filter in production
    if (!builder.Environment.IsDevelopment())
    {
        // options.Filters.Add<GlobalExceptionFilter>();
    }
})
.ConfigureApiBehaviorOptions(options =>
{
    // Configure DateTime parsing behavior
    options.SuppressModelStateInvalidFilter = false;
});

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

try
{
    logger.LogInformation("ShoesEcommerce application starting up...");

    // Seed database with initial data
    try
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Ensure database is created and up to date
        await context.Database.EnsureCreatedAsync();
        
        // Seed initial data using the correct method
        await DataSeeder.SeedAllDataAsync(context);
        
        logger.LogInformation("Database seeded successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database");
        // Don't rethrow - allow app to continue without seed data
    }

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        // Use custom error pages in production
        app.UseExceptionHandler("/Error");
        app.UseStatusCodePagesWithReExecute("/Error/{0}");
        
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
        
        logger.LogInformation("Production error handling configured");
    }
    else
    {
        app.UseDeveloperExceptionPage();
        logger.LogInformation("Developer exception page enabled");
    }

    //// Initialize Firebase Admin SDK load file firebase-adminsdk.json
    //try
    //{
    //    FirebaseApp.Create(new AppOptions()
    //    {
    //        Credential = GoogleCredential.FromFile("wwwroot/credentials/shoes-ecommerce-fd0cb-firebase-adminsdk-fbsvc-b9bf519edf.json"),
    //    });
    //    logger.LogInformation("Firebase Admin SDK initialized successfully");
    //}
    //catch (Exception ex)
    //{
    //    logger.LogError(ex, "Failed to initialize Firebase Admin SDK");
    //    // Continue without Firebase if initialization fails
    //}

    //Add Sessions 
    app.UseSession(); // Enable session middleware

    // 🕐 ADD: Use request localization for consistent DateTime handling
    app.UseRequestLocalization();

    // Add enhanced error logging middleware early in the pipeline
    app.UseErrorLogging();
    
    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    // ENHANCED ROUTING - Admin controllers with proper naming
    
    // Admin-specific routes - these must come BEFORE the default route
    app.MapControllerRoute(
        name: "admin_product",
        pattern: "Admin/Product/{action=Index}/{id?}",
        defaults: new { controller = "AdminProduct" });

    app.MapControllerRoute(
        name: "admin_stock",
        pattern: "Admin/Stock/{action=Index}/{id?}",
        defaults: new { controller = "AdminStock" });

    app.MapControllerRoute(
        name: "admin_staff",
        pattern: "Admin/Staff/{action=Index}/{id?}",
        defaults: new { controller = "AdminStaff" });

    app.MapControllerRoute(
        name: "admin_order",
        pattern: "Admin/Order/{action=Index}/{id?}",
        defaults: new { controller = "AdminOrder" });

    app.MapControllerRoute(
        name: "admin_discount",
        pattern: "Admin/Discount/{action=Index}/{id?}",
        defaults: new { controller = "AdminDiscount" });

    app.MapControllerRoute(
        name: "admin_data",
        pattern: "Admin/Data/{action=Index}/{id?}",
        defaults: new { controller = "Data" });

    // General admin route
    app.MapControllerRoute(
        name: "admin_default",
        pattern: "Admin/{action=Index}/{id?}",
        defaults: new { controller = "Admin" });

    // Default route for all other controllers
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    // Custom error handling routes
    app.MapControllerRoute(
        name: "error",
        pattern: "Error/{statusCode?}",
        defaults: new { controller = "Error", action = "HandleErrorCode" });

    logger.LogInformation("Routing configured successfully");

    // Add health check endpoint for monitoring
    app.MapGet("/health", () => 
    {
        logger.LogDebug("Health check endpoint accessed");
        return Results.Ok(new { 
            status = "healthy", 
            timestamp = DateTime.UtcNow,
            environment = app.Environment.EnvironmentName,
            version = "1.0.0"
        });
    });

    logger.LogInformation("ShoesEcommerce application started successfully");

    app.Run();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Application terminated unexpectedly during startup");
    throw;
}
