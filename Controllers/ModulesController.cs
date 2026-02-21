using Microsoft.AspNetCore.Mvc;

namespace DEEPFAKE.Controllers
{
    public class ModulesController : Controller
    {
        public IActionResult Email()
        {
            return View();
        }

        public IActionResult Media()
        {
            return View();
        }

        public IActionResult Website()
        {
            return View();
        }

        public IActionResult Scam()
        {
            return View();
        }
    }
}
