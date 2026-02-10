using Microsoft.AspNetCore.Mvc;

namespace CardLister.Web.Controllers
{
    /// <summary>
    /// Export feature is disabled in the web app.
    /// Use the CardLister Desktop application to export cards to CSV.
    /// </summary>
    public class ExportController : Controller
    {
        public IActionResult Index()
        {
            TempData["ErrorMessage"] = "CSV export is only available in the CardLister Desktop application.";
            return RedirectToAction("Index", "Scan");
        }
    }
}
