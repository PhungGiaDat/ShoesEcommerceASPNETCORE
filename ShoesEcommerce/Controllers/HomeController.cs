using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ShoesEcommerce.Models;
using ShoesEcommerce.Services.Interfaces;

namespace ShoesEcommerce.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IProductService _productService;

        public HomeController(ILogger<HomeController> logger, IProductService productService)
        {
            _logger = logger;
            _productService = productService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Get featured product variants instead of products
                var featuredVariants = await _productService.GetFeaturedProductVariantsAsync(8);
                ViewBag.FeaturedProductVariants = featuredVariants;
                
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading home page data");
                ViewBag.FeaturedProductVariants = new List<object>(); // Empty list on error
                return View();
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // Fixed browser probe handlers - separate routes for each pattern
        [HttpGet("/.well-known/{path}")]
        public IActionResult HandleWellKnown(string path)
        {
            // Return 204 No Content for .well-known requests
            return NoContent();
        }

        [HttpGet("/favicon.ico")]
        public IActionResult HandleFavicon()
        {
            // Return 204 No Content for favicon requests
            return NoContent();
        }

        [HttpGet("/robots.txt")]
        public IActionResult HandleRobots()
        {
            // Return 204 No Content for robots.txt requests
            return NoContent();
        }

        [HttpGet("/{filename}.map")]
        public IActionResult HandleSourceMaps(string filename)
        {
            // Return 204 No Content for source map requests
            return NoContent();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
