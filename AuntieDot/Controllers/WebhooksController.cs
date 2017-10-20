using System;
using System.Data.Entity;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using AuntieDot.Models;
using Microsoft.AspNet.Identity.Owin;
using Newtonsoft.Json;
using ShopifySharp;
using Order = ShopifySharp.Order;

namespace AuntieDot.Controllers {
    public class WebhooksController : Controller {
        [HttpPost]
        public async Task<string> OrderUpdated(string userId) {
            var isValidRequest = await AuthorizationService.IsAuthenticWebhook(
                Request.Headers.ToKvps(),
                Request.InputStream,
                ApplicationEngine.ShopifySecretKey);
            if (!isValidRequest)
                throw new UnauthorizedAccessException("This request is not an authentic webhook request.");
            //ShopifySharp has just read the input stream. We must always reset the inputstream     
            //before reading it again.    
            Request.InputStream.Position = 0;
            //Do not dispose the StreamReader or input stream. The controller will do that itself.
            var bodyText = await new StreamReader(Request.InputStream).ReadToEndAsync();
            //Parse the ShopifyOrder from the body text    
            var shopifyOrder = JsonConvert.DeserializeObject<Order>(bodyText);
            // TODO 
            using (var db = new ApplicationDbContext()) {
                var ordersRef = db.GetUserOrdersReference(userId);
                var order = await ordersRef.Query().FirstOrDefaultAsync(o => o.ShopifyId == shopifyOrder.Id.Value);
                //Shopify often sends this webhook before the OrderCreated webhook.         
                //Check that the order actually exists in the database before trying to update it.
                if (order == null) return "Order does not exist in database.";
                //Transfer the updated order's properties to the database order        
                var updatedOrder = shopifyOrder.ToDatabaseOrder();
                order.CustomerName = updatedOrder.CustomerName;
                order.DateCreated = updatedOrder.DateCreated;
                order.DisplayId = updatedOrder.DisplayId;
                order.IsOpen = updatedOrder.IsOpen;
                order.LineItemSummary = updatedOrder.LineItemSummary;
                await db.SaveChangesAsync();
            }
            return "Successfully handled OrderUpdated webhook.";
        }


        [HttpPost]
        [Route("webhooks/orders/create")]
        public async Task<string> OrderCreated(string userId) {
            var isValidRequest = await AuthorizationService.IsAuthenticWebhook(
                Request.Headers.ToKvps(),
                Request.InputStream,
                ApplicationEngine.ShopifySecretKey);
            if (!isValidRequest)
                throw new UnauthorizedAccessException("This request is not an authentic webhook request.");

            //ShopifySharp has just read the input stream. We must always reset the inputstream     
            //before reading it again.    
            Request.InputStream.Position = 0;
            //Do not dispose the StreamReader or input stream. The controller will do that itself.
            var bodyText = await new StreamReader(Request.InputStream).ReadToEndAsync();
            //Parse the ShopifyOrder from the body text    
            var shopifyOrder = JsonConvert.DeserializeObject<Order>(bodyText);
            //Convert the order to one that can be stored in the database    
            var order = shopifyOrder.ToDatabaseOrder();
            //Get a reference to the user's .Orders collection and save this one to the database    
            using (var db = new ApplicationDbContext()) {
                var ordersRef = db.GetUserOrdersReference(userId);
                ordersRef.CurrentValue.Add(order);
                await db.SaveChangesAsync();
            }
            return "Successfully handled OrderCreated webhook.";
        }


        public async Task<string> AppUninstalled(string userId) {
            var isValidRequest = await AuthorizationService.IsAuthenticWebhook(
                Request.Headers.ToKvps(),
                Request.InputStream,
                ApplicationEngine.ShopifySecretKey);
            if (!isValidRequest)
                throw new UnauthorizedAccessException("This request is not an authentic webhook request.");
            //Pull in the user
            var owinContext = HttpContext.GetOwinContext();
            var usermanager = owinContext.GetUserManager<ApplicationUserManager>();
            var user = await usermanager.FindByIdAsync(userId);
            //Delete their subscription charge and Shopify details
            user.ShopifyChargeId = null;
            user.ShopifyAccessToken = null;
            user.MyShopifyDomain = null;
            //Save changes
            var update = await usermanager.UpdateAsync(user);
            if (!update.Succeeded) {
                // TODO: Log or handle exception in whatever way you see fit.
                var message = "Couldn't delete a user's Shopify details. Reason: " +
                              string.Join(", ", update.Errors);
                throw new Exception(message);
            }
            CacheEngine.ResetShopStatus(user.Id, HttpContext);
            //TODO: Send email to custer asking why they uninstalled your app
            return "Successfully handled AppUninstalled webhook.";
        }
    }
}