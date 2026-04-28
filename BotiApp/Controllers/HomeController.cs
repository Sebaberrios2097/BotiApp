using BotiApp.Models;
using Infraestructura.Repositories.Sp.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace BotiApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly IBotiAppStoredProcedures _sp;

        public HomeController(IBotiAppStoredProcedures sp)
        {
            _sp = sp;
        }
        public async Task<IActionResult> Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult AccesoDenegado()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
