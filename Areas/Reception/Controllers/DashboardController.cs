using Microsoft.AspNetCore.Mvc;

namespace QuanLyKTX.Areas.Reception.Controllers
{
    [Area("Reception")]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}