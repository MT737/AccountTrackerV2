using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountTrackerV2.Controllers
{
    public class GeneralItemsController : Controller
    {
        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }
    }
}