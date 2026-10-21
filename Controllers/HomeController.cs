using Microsoft.AspNetCore.Mvc;

namespace QLCC_LTW.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Landing()
        {
            return View();
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}