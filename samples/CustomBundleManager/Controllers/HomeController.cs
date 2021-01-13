using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CustomBundleManager.Models;

namespace CustomBundleManager.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index(string model)
        {
            return View((object)model);
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
