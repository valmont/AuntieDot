using System.Web.Mvc;
using AuntieDot.Attributes;

namespace AuntieDot.Controllers {
    [RequireSubscription]
    public class DashboardController : Controller {
        // GET: Dashboard
        public ActionResult Index() {
            return View();
        }
    }
}