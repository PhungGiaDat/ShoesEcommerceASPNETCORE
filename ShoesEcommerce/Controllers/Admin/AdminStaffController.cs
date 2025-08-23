using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ShoesEcommerce.Services.Interfaces;

namespace ShoesEcommerce.Controllers.Admin
{
    [Authorize(Roles = "Admin,Staff")]
    public class AdminStaffController : Controller
    {
        private readonly IStaffService _staffService;
        private readonly ILogger<AdminStaffController> _logger;

        public AdminStaffController(IStaffService staffService, ILogger<AdminStaffController> logger)
        {
            _staffService = staffService;
            _logger = logger;
        }

        // GET: Admin/Staff
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Quản lý Nhân viên - Admin";
            return View();
        }

        // GET: Admin/Staff/Details/5
        public async Task<IActionResult> Details(int id)
        {
            ViewData["Title"] = "Chi tiết Nhân viên - Admin";
            return View();
        }
    }
}