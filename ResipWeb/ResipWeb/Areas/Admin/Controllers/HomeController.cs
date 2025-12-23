using Microsoft.AspNetCore.Mvc;

namespace ResipWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // KHÔNG Authorize ở đây
            return RedirectToAction("Index", "Admin");
        }
    }
}
