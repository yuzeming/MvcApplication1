using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MvcApplication1.Filters;
using MvcApplication1.Models;

namespace MvcApplication1.Controllers
{

    [InitializeSimpleMembership]
    public class HomeController : Controller
    {
        private MyDbContext db = new MyDbContext();

        public ActionResult Index()
        {
            ViewBag.c = db.Contests.Where(x => x.Start < DateTime.Now && x.End > DateTime.Now 
                && x.UserList.Any(z => z.UserName == User.Identity.Name)).ToList();
            return View();
        }
    }
}
