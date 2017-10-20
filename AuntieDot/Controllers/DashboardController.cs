using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using AuntieDot.Attributes;
using AuntieDot.Models;
using Microsoft.AspNet.Identity;

namespace AuntieDot.Controllers {
    [RequireSubscription]
    public class DashboardController : Controller {
        private readonly ApplicationDbContext _database;

        public DashboardController() {
            _database = new ApplicationDbContext();
        }

        // GET: Dashboard
        public async Task<ActionResult> Index(int page = 1) {
            var pageSize = 50;
            //Get a reference and a query for the user's orders    
            var ordersRef = _database.GetUserOrdersReference(User.Identity.GetUserId());
            var query = ordersRef.Query();
            //Figure out how many orders the user has, and how many pages of 50 they'll create    
            var totalOrders = await query.CountAsync();
            var totalPages = totalOrders % pageSize > 0
                ? totalOrders / pageSize + 1
                : totalOrders / pageSize;
            //Pass those values to the ViewBag    
            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalPages = totalPages;
            //Page must be at least 1    
            page = page < 1 ? 1 : page;
            //Get the current page of orders    
            var orders = await query.OrderByDescending(o => o.DateCreated).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return View(orders);
        }

        protected override void Dispose(bool disposing) {
            if (disposing) _database.Dispose();
            base.Dispose(disposing);
        }
    }
}