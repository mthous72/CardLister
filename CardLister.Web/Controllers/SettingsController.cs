using Microsoft.AspNetCore.Mvc;

namespace CardLister.Web.Controllers
{
    /// <summary>
    /// Settings feature is disabled in the web app.
    /// Use the CardLister Desktop application to configure API keys and preferences.
    /// </summary>
    public class SettingsController : Controller
    {
        public IActionResult Index()
        {
            TempData["ErrorMessage"] = "Settings are only available in the CardLister Desktop application. Configure API keys and preferences there.";
            return RedirectToAction("Index", "Scan");
        }
    }
}
