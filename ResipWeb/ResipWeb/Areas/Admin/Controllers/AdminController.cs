using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResipWeb.Models;

namespace ResipWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class AdminController : Controller
    {
        // 🔥 1. KHAI BÁO CONTEXT
        private readonly AppDbContext _context;

        // 🔥 2. INJECT QUA CONSTRUCTOR
        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // 🔥 3. ACTION INDEX
        public IActionResult Index()
        {
            // SẢN PHẨM
            ViewBag.ProductCount = _context.SanPhams.Count();
            ViewBag.ActiveProducts = _context.SanPhams.Count(p => p.IsActive);
            ViewBag.InactiveProducts = _context.SanPhams.Count(p => !p.IsActive);

            // DANH MỤC
            ViewBag.CategoryCount = _context.Categories.Count();
            ViewBag.ActiveCategories = _context.Categories.Count(c => c.IsActive);

            // CHƯA LÀM → 0
            ViewBag.PendingOrders = 0;
            ViewBag.CompletedOrders = 0;
            ViewBag.TotalCustomers = 0;
            ViewBag.TotalRevenue = 0;

            // 🔥 TOP DANH MỤC – ĐÚNG THEO DB BẠN
            ViewBag.TopCategories = _context.Categories
                .Select(c => new
                {
                    CategoryName = c.Name,
                    ProductCount = _context.SanPhams.Count(p => p.CategoryId == c.Id)
                })
                .OrderByDescending(x => x.ProductCount)
                .Take(5)
                .ToList();

            return View();
        }
    }
}
