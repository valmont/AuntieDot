using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using ShopifySharp;

namespace AuntieDot.Controllers
{
    public class ProxyController : Controller
    {
        // GET: Proxy
        public async Task<ActionResult> Index(string shop, string path_prefix) {
            if (!AuthorizationService.IsAuthenticProxyRequest(Request.QueryString.ToKvps(), ApplicationEngine.ShopifySecretKey)) {
                throw new UnauthorizedAccessException("This request is not an authentic p roxy page request.");
            }

            //Let Shopify render the returned view with Liquid.            
            Response.ContentType = "application/liquid";
            return View();
        }
    }
}