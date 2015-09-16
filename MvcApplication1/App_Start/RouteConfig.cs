using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace MvcApplication1
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "ProblemContest",
                url: "{controller}/{id}/{action}/{fn}",
                defaults: new {  fn = UrlParameter.Optional },
                constraints: new { id = @"\d+" , controller = "Problem|Contest|Submit", }
            );

            routes.MapRoute(
                name: "Default1",
                url: "{controller}/{action}/",
                defaults: new { controller = "Home", action = "Index" }
            );





        }
    }
}