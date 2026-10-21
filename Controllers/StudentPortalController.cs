using Microsoft.AspNetCore.Mvc;

namespace QuanLyKTX.Controllers
{
    public class StudentPortalController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
