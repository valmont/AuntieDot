using System.Web.Mvc;

namespace AuntieDot.Controllers {
    public class IframeBreakerController : Controller {
        public ActionResult Index(string url) {
            ViewBag.Url = url;
            return View();
        }
    }
}