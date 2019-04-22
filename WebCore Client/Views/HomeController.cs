using Microsoft.AspNetCore.Mvc;

namespace WebCore_Client.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
    }
}
