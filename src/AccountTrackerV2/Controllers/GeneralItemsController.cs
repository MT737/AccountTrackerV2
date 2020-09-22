using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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