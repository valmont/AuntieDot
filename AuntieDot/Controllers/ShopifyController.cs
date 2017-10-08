using System;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using AuntieDot.Models;
using Microsoft.AspNet.Identity.Owin;
using ShopifySharp;

namespace AuntieDot.Controllers {
    public class ShopifyController : Controller {
        public async Task<ActionResult> Handshake(string shop) {
            //Store the shop URL in a cookie.
            Response.SetCookie(new HttpCookie(".App.Handshake.ShopUrl", shop) {
                Expires = DateTime.Now.AddDays(30)
            });
            //Open a connection to our database
            using (var db = new ApplicationDbContext()) {
                //Check if any user in the database has already connected this shop.
                if (await db.Users.AnyAsync(user => user.MyShopifyDomain == shop))
                    return RedirectToAction("Index", "Dashboard");
                //This shop does not exist, and the user is trying to install the app.
                //Redirect them to registration.
                return RedirectToAction("Index", "Register");
            }
        }

        [Authorize]
        public async Task<ActionResult> AuthResult(string shop, string code) {
            var apiKey = ApplicationEngine.ShopifyApiKey;
            var secretKey = ApplicationEngine.ShopifySecretKey;
            //Validate the signature of the request to ensure that it's valid
            if (!AuthorizationService.IsAuthenticRequest(Request.QueryString.ToKvps(), secretKey))
                throw new Exception("You've been a bad boy.");
            //The request is valid. Exchange the temporary code for a permanent access token
            string accessToken;
            try {
                accessToken = await AuthorizationService.Authorize(code, shop, apiKey, secretKey);
            }
            catch (ShopifyException e) {
                // Failed to authorize app installation.
                // TODO: Log or handle exception in whatever way you see fit.
                throw e;
            }
            //Get the user
            var usermanager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            var user = await usermanager.FindByNameAsync(User.Identity.Name);
            user.ShopifyAccessToken = accessToken;
            user.MyShopifyDomain = shop;
            // TODO: Create the AppUninstalled webhook in Chapter 6.
            var update = await usermanager.UpdateAsync(user);
            if (!update.Succeeded) {
                // TODO: Log or handle exception in whatever way you see fit.
                var message = "Couldn't save a user's access token and shop domain. Reason: "
                              +
                              string.Join(", ", update.Errors);
                throw new Exception(message);
            }
            //Create the AppUninstalled webhook
            var service = new WebhookService(user.MyShopifyDomain, user.ShopifyAccessToken
            );
            var hook = new Webhook {
                Address = "https://auntiedot.apphb.com/webhooks/appuninstalled?userId=" + user.Id,
                Topic = "app/uninstalled"
            };
            try {
                hook = await service.CreateAsync(hook);
            }
            catch (ShopifyException e) when (e.Message.ToLower().Contains("for this topic has already been taken")) {
                //Ignore error, webhook has already been created and is still valid.
            }
            catch (ShopifyException e) {
                // TODO: Log or handle exception in whatever way you see fit.
                throw e;
            }

            //Delete the shop's status from cache to force a refresh.
            CacheEngine.ResetShopStatus(user.Id, HttpContext);
            return RedirectToAction("Charge", "Register");
        }

        [Authorize]
        public async Task<ActionResult> ChargeResult(string shop, long charge_id) {
            //Get the user
            var owinContext = HttpContext.GetOwinContext();
            var usermanager = owinContext.GetUserManager<ApplicationUserManager>();
            var user = await usermanager.FindByNameAsync(User.Identity.Name);
            //Create the billing service, which will be used to pull in the charge id
            var service = new RecurringChargeService(user.MyShopifyDomain, user.ShopifyAccessToken);
            RecurringCharge charge;
            //Try to get the charge. If a "404 Not Found" exception is thrown, the charge has been deleted.
            try {
                charge = await service.GetAsync(charge_id);
            }
            catch (ShopifyException e)
                when ((int) e.HttpStatusCode == 404 /* Not found */) {
                //The charge has been deleted. Redirect the user to accept a new charge.
                return RedirectToAction("Charge", "Register");
            }
            //Activate the charge
            await service.ActivateAsync(charge_id);
            //Get the charge again to refresh its BillingOn date.
            charge = await service.GetAsync(charge_id);
            //Save the charge to the user model
            user.ShopifyChargeId = charge_id;
            user.BillingOn = charge.BillingOn;
            var update = await usermanager.UpdateAsync(user);

            if (!update.Succeeded) {
                // TODO: Log or handle exception in whatever way you see fit.
                var message = "Couldn't save a user's activated charge id. Reason: " +
                              string.Join(", ", update.Errors);
                throw new Exception(message);
            }
            //Delete the shop's status from cache to force a refresh.
            CacheEngine.ResetShopStatus(user.Id, HttpContext);
            //User's subscription charge has been activated and they can now use the app.
            return RedirectToAction("Index", "Dashboard");
        }
    }
}