using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using AuntieDot.Attributes;
using AuntieDot.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using ShopifySharp;
using ShopifySharp.Filters;
using Order = ShopifySharp.Order;

namespace AuntieDot.Controllers {
    [RequireSubscription]
    public class DashboardController : Controller {
        private readonly ApplicationDbContext _database;

        public DashboardController() {
            _database = new ApplicationDbContext();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SaveWidgetCustomization(string title, string blurb, string hexColor) {
            //Grab the user model    
            var owinContext = HttpContext.GetOwinContext();
            var usermanager = owinContext.GetUserManager<ApplicationUserManager>();
            var user = await usermanager.FindByNameAsync(User.Identity.Name);
            //Save the widget properties    
            user.WidgetTitle = title;
            user.WidgetBlurb = blurb;
            user.WidgetHexColor = hexColor;
            //Check if we need to create a script tag    
            if (user.ScriptTagId.HasValue == false) {
                var service = new ScriptTagService(user.MyShopifyDomain, user.ShopifyAccessToken);
                var tag = new ScriptTag {
                    Event = "onload",
                    Src = "https://auntiedot.apphb.com/scripts/email-widget.js"
                };
                tag = await service.CreateAsync(tag);
                //Save the tag id to the user's model        
                user.ScriptTagId = tag.Id;
            }
            //Save changes    
            var save = await usermanager.UpdateAsync(user);
            if (!save.Succeeded)
                throw new Exception("Failed to save widget settings. Reasons: " + string.Join(", ", save.Errors));
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Import() {
            var shop = CacheEngine.GetShopStatus(User.Identity.GetUserId(), HttpContext);
            //Get a total count of open orders on the shop    
            var service = new OrderService(shop.MyShopifyDomain, shop.ShopifyAccessToken);
            var count = await service.CountAsync(new OrderFilter {
                Status = "open"
            });
            var page = 0;
            var importedOrders = new List<Order>();
            while (page <= count / 250) {
                page += 1;
                //To reduce bandwidth/time, only return the fields that we'll be using.        
                //These field names MUST match the Shopify API, NOT the ones in ShopifySharp.        
                var filterOptions = new OrderFilter {
                    Limit = 250,
                    Status = "open",
                    Fields = "id,created_at,name,line_items,customer",
                    Page = page
                };
                importedOrders.AddRange(await service.ListAsync(filterOptions));
            }
            //Get a reference to the user's orders.    
            var orderRef = _database.GetUserOrdersReference(User.Identity.GetUserId());
            //Get a list of all of the user's orders in this list that already exist in the datab ase    
            var ids = importedOrders.Select(i => i.Id ?? 0);
            var existing = await orderRef.Query()
                .Where(o => ids.Contains(o.ShopifyId))
                .Select(o => o.ShopifyId)
                .ToListAsync();
            //Use the list of existing order ids to determine which ones need to be added    
            importedOrders = importedOrders.Where(s => existing.Contains(s.Id.Value) == false).ToList();
            //Convert the final list of orders into a database order and save them    
            foreach (var order in importedOrders) orderRef.CurrentValue.Add(order.ToDatabaseOrder());

            await _database.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Fulfill(long id, IEnumerable<long> items, string company, string number,
            string url) {
            var shop = CacheEngine.GetShopStatus(User.Identity.GetUserId(), HttpContext);
            var fulfillService = new FulfillmentService(shop.MyShopifyDomain, shop.ShopifyAccessToken);
            var orderService = new OrderService(shop.MyShopifyDomain, shop.ShopifyAccessToken);
            Fulfillment fulfillment;
            //Get a reference to the user's orders, then find the requested order's id.    
            var orderRef = _database.GetUserOrdersReference(User.Identity.GetUserId());
            var shopifyId = await orderRef.Query().Where(o => o.Id == id).Select(o => o.ShopifyId).FirstAsync();
            //Pull in the full Shopify order, which includes the Shopify customer.    
            var fullOrder = await orderService.GetAsync(shopifyId);
            if (fullOrder.FulfillmentStatus == "fulfilled") return RedirectToAction("Order", new {id});
            //Get a list of all of the line items that are going to be fulfilled but aren't part of an    
            //already pending fulfillment    
            var toFulfill = fullOrder.LineItems
                .Where(li => items.Contains(li.Id.Value))
                .Where(li => fullOrder.Fulfillments.Where(f => f.Status == "pending")
                                 .Any(f => f.LineItems.Any(fLineItem => fLineItem.Id == li.Id)) == false);
            //Create a new fulfillment that will fulfill the remaining line items    
            var trackingNumber = Guid.NewGuid().ToString();
            fulfillment = new Fulfillment {
                TrackingCompany = company ?? "AuntieDot Shipping, LLC.",
                TrackingNumber = number ?? trackingNumber,
                TrackingUrl = url ?? "https://shipping-company.com/track/" + trackingNumber,
                OrderId = fullOrder.Id.Value,
                LineItems = toFulfill
            };
            //Send the fulfillment to Shopify    
            var notifyCustomer = true;
            fulfillment = await fulfillService.CreateAsync(shopifyId, fulfillment, notifyCustomer);

            if (fulfillment.Status != "pending" && fulfillment.Status != "success")
                throw new Exception("Failed to fulfill line items.");

            return RedirectToAction("Order", new {id});
        }

        public async Task<ActionResult> Order(int id) {
            //Get a reference to the user's orders, then find the requested order's ShopifyId.    
            var orderRef = _database.GetUserOrdersReference(User.Identity.GetUserId());
            var shopifyId = await orderRef.Query().Where(o => o.Id == id).Select(o => o.ShopifyId).FirstAsync();
            //Get their cached shop data so we can use their access tokens without making another    
            //query to the database.
            var shop = CacheEngine.GetShopStatus(User.Identity.GetUserId(), HttpContext);
            //Pull in the full Shopify order, which includes the Shopify customer.    
            var orderService = new OrderService(shop.MyShopifyDomain, shop.ShopifyAccessToken);
            var fullOrder = await orderService.GetAsync(shopifyId);
            //Pass the database order id to the ViewBag    ViewBag.OrderId = id;
            return View(fullOrder);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SetStatus(int id, string status = "closed") {
            var shop = CacheEngine.GetShopStatus(User.Identity.GetUserId(), HttpContext);
            var orderService = new OrderService(shop.MyShopifyDomain, shop.ShopifyAccessToken);
            //Get a reference to the user's orders, then find the requested order's id.    
            var orderRef = _database.GetUserOrdersReference(User.Identity.GetUserId());
            var shopifyId = await orderRef.Query().Where(o => o.Id == id).Select(o => o.ShopifyId).FirstAsync();
            if (status.Equals("open", StringComparison.OrdinalIgnoreCase)) await orderService.OpenAsync(shopifyId);
            else await orderService.CloseAsync(shopifyId);
            return RedirectToAction("Order", new {id});
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
            var orders = await query.OrderByDescending(o => o.DateCreated).Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();
            var owinContext = HttpContext.GetOwinContext();
            var usermanager = owinContext.GetUserManager<ApplicationUserManager>();
            var user = await usermanager.FindByNameAsync(User.Identity.Name);
            ViewBag.User = user;
            return View(orders);
        }

        protected override void Dispose(bool disposing) {
            if (disposing) _database.Dispose();
            base.Dispose(disposing);
        }
    }
}