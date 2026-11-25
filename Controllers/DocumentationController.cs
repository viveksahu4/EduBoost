using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduBoost.Controllers
{
    [AllowAnonymous]
    public class DocumentationController : Controller
    {
        // GET: /Documentation
        public IActionResult Index()
        {
            ViewData["Title"] = "EduBoost Documentation";
            return View();
        }
    }
}
