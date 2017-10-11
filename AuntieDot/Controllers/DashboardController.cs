using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using AuntieDot.Attributes;
using Microsoft.AspNet.Identity.Owin;

namespace AuntieDot.Controllers {
    [RequireSubscription]
    public class DashboardController : Controller {
        // GET: Dashboard
        public async Task<ActionResult> Index() {
            var owinContext = HttpContext.GetOwinContext();
            var usermanager = owinContext.GetUserManager<ApplicationUserManager>();
            var user = await usermanager.FindByNameAsync(User.Identity.Name);
            if (user == null) {
                return new HttpUnauthorizedResult();
            }
            return View(user);
        }
    }
}