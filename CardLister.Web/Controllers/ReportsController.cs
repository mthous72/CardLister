using Microsoft.AspNetCore.Mvc;

namespace CardLister.Web.Controllers
{
    /// <summary>
    /// Reports feature is disabled in the web app.
    /// Use the CardLister Desktop application for full reporting capabilities.
    /// </summary>
    public class ReportsController : Controller
    {
        public IActionResult Index()
        {
            TempData["ErrorMessage"] = "Reports are only available in the CardLister Desktop application. The web app provides card scanning and pricing research only.";
            return RedirectToAction("Index", "Scan");
        }
    }
}
