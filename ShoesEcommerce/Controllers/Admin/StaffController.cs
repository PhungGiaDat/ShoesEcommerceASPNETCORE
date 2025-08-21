using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ShoesEcommerce.Services.Interfaces;

namespace ShoesEcommerce.Controllers.Admin
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")] // Require Admin or Staff role
    public class StaffController : Controller
    {
        private readonly IStaffService _staffService;
        private readonly ILogger<StaffController> _logger;

        public StaffController(IStaffService staffService, ILogger<StaffController> logger)
        {
            _staffService = staffService;
            _logger = logger;
        }

        // GET: Admin/Staff
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Qu?n l� Nh�n vi�n - Admin";
            return View();
        }

        // GET: Admin/Staff/Details/5
        public async Task<IActionResult> Details(int id)
        {
            ViewData["Title"] = "Chi ti?t Nh�n vi�n - Admin";
            return View();
        }
    }
}