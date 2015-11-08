using System;
using Microsoft.AspNet.Mvc;

namespace UiT.Inf3200.FrontendApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Nodes");
        }

        public IActionResult Error()
        {
            throw new Exception("This is the developer exception page.");
        }
    }
}
