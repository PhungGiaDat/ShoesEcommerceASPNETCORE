using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ShoesEcommerce.Data;

namespace ShoesEcommerce.Controllers
{
    [Authorize(Roles = "Admin,Staff")] // Simple role-based authorization
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(AppDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Trang ch? Admin";
            
            try
            {
                // Test database connection
                await _context.Database.CanConnectAsync();
                _logger.LogInformation("Database connection successful");
                
                // Get basic counts
                var staffCount = await _context.Staffs.CountAsync();
                var departmentCount = await _context.Departments.CountAsync();
                var roleCount = await _context.Roles.CountAsync();
                
                ViewBag.StaffCount = staffCount;
                ViewBag.DepartmentCount = departmentCount;
                ViewBag.RoleCount = roleCount;
                ViewBag.DatabaseStatus = "Connected";
                
                _logger.LogInformation("Dashboard loaded successfully with {StaffCount} staff, {DepartmentCount} departments, {RoleCount} roles", 
                    staffCount, departmentCount, roleCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                ViewBag.DatabaseStatus = "Error: " + ex.Message;
                ViewBag.StaffCount = 0;
                ViewBag.DepartmentCount = 0;
                ViewBag.RoleCount = 0;
            }
            
            return View();
        }

        public IActionResult Staff()
        {
            ViewData["Title"] = "Qu?n lý Nhân viên";
            return View();
        }

        public IActionResult Customer()
        {
            ViewData["Title"] = "Khách hàng";
            return View();
        }

        public IActionResult Inventory()
        {
            ViewData["Title"] = "Qu?n lý T?n kho";
            return View();
        }

        public IActionResult Orders()
        {
            ViewData["Title"] = "Qu?n lý ??n hàng";
            return View();
        }

        // Health check endpoint
        public async Task<IActionResult> HealthCheck()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                var staffCount = await _context.Staffs.CountAsync();
                var departmentCount = await _context.Departments.CountAsync();
                var roleCount = await _context.Roles.CountAsync();

                return Json(new
                {
                    status = "healthy",
                    database = canConnect ? "connected" : "disconnected",
                    staffCount,
                    departmentCount,
                    roleCount,
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return Json(new
                {
                    status = "unhealthy",
                    error = ex.Message,
                    timestamp = DateTime.Now
                });
            }
        }
    }
}