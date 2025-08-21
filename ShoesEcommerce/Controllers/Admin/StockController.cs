using Microsoft.AspNetCore.Mvc;
using ShoesEcommerce.Services.Interfaces;

namespace ShoesEcommerce.Controllers.Admin
{
    [Area("Admin")]
    public class StockController : Controller
    {
        private readonly IStockService _stockService;
        private readonly ILogger<StockController> _logger;

        public StockController(IStockService stockService, ILogger<StockController> logger)
        {
            _stockService = stockService;
            _logger = logger;
        }

        // GET: Admin/Stock/Inventory
        public async Task<IActionResult> Inventory()
        {
            ViewData["Title"] = "Qu?n lý T?n kho - Admin";
            return View();
        }

        // GET: Admin/Stock/Import
        public async Task<IActionResult> Import()
        {
            ViewData["Title"] = "Nh?p hàng - Admin";
            return View();
        }

        // GET: Admin/Stock/Check
        public async Task<IActionResult> Check()
        {
            ViewData["Title"] = "Ki?m kho - Admin";
            return View();
        }
    }
}