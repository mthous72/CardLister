using Microsoft.AspNetCore.Mvc;

namespace CardLister.Web.Controllers
{
    /// <summary>
    /// Inventory management is disabled in the web app.
    /// Use the CardLister Desktop application for full inventory management features.
    /// </summary>
    public class InventoryController : Controller
    {
        public IActionResult Index()
        {
            TempData["ErrorMessage"] = "Inventory management is only available in the CardLister Desktop application. The web app provides card scanning and pricing research only.";
            return RedirectToAction("Index", "Scan");
        }
    }
}
