using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoesEcommerce.Data;

namespace ShoesEcommerce.Controllers.Admin
{
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
            ViewData["Title"] = "Trang chủ Admin";
            
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

        // Redirect to StaffController for staff management
        public IActionResult Staff()
        {
            return RedirectToAction("Index", "Staff");
        }

        public IActionResult Customer()
        {
            ViewData["Title"] = "Khách hàng";
            return View();
        }

        public IActionResult Pending()
        {
            ViewData["Title"] = "Đơn hàng Chờ Phê duyệt";
            return View();
        }

        public IActionResult Confirmed()
        {
            ViewData["Title"] = "Đơn hàng Đã Xác nhận";
            return View();
        }

        public IActionResult Completed()
        {
            ViewData["Title"] = "Đơn hàng Thành công";
            return View();
        }

        public IActionResult Inventory()
        {
            ViewData["Title"] = "Quản lý Tồn kho";
            return View();
        }

        public IActionResult Check()
        {
            ViewData["Title"] = "Kiểm kho";
            return View();
        }

        public IActionResult Import()
        {
            ViewData["Title"] = "Nhập hàng";
            return View();
        }

        public IActionResult Dashboard()
        {
            ViewData["Title"] = "Bảng điều khiển Admin";
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
