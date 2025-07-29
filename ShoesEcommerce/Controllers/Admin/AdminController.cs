using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;

namespace ShoesEcommerce.Controllers.Admin
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Trang chủ Admin";
            return View();
        }

        public IActionResult Staff()
        {
            ViewData["Title"] = "Nhân viên";
            return View();
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
    }
}
