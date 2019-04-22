using Microsoft.AspNetCore.Mvc;

namespace WebCore_Server.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
    }
}
