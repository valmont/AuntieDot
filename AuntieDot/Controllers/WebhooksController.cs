using System;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity.Owin;
using ShopifySharp;

namespace AuntieDot.Controllers {
    public class WebhooksController : Controller {
        public async Task<string> AppUninstalled(string userId) {
            var isValidRequest = await AuthorizationService.IsAuthenticWebhook(
                Request.Headers.ToKvps(),
                Request.InputStream,
                ApplicationEngine.ShopifySecretKey);
            if (!isValidRequest) {
                throw new UnauthorizedAccessException("This request is not an authentic webhook request.");
            }
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
                string message = "Couldn't delete a user's Shopify details. Reason: " +
                                 string.Join(", ", update.Errors);
                throw new Exception(message);
            }
            CacheEngine.ResetShopStatus(user.Id, HttpContext);
            //TODO: Send email to custer asking why they uninstalled your app
            return "Successfully handled AppUninstalled webhook.";
        }
    }
}