using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace AuntieDot {
    public class RouteConfig {
        public static void RegisterRoutes(RouteCollection routes) {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "WHOU",
                url: "webhooks/orders/updated",
                defaults: new { controller = "Webhooks", action = "OrderUpdated", userId = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "WHOC",
                url: "webhooks/orders/create",
                defaults: new { controller = "Webhooks", action = "OrderCreated", userId = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
