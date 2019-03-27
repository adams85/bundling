using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DynamicBundle.Models;

namespace DynamicBundle.Controllers
{
    public class HomeController : Controller
    {
        readonly DynamicSourceInvalidator _invalidator;

        public HomeController(DynamicSourceInvalidator invalidator)
        {
            _invalidator = invalidator;
        }

        public IActionResult Index(string model)
        {
            return View((object)model);
        }

        [HttpPost]
        public IActionResult Invalidate(string model)
        {
            _invalidator.Invalidate();
            return RedirectToAction(nameof(Index), new { model });
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
