using System.Web.Mvc;
using System.Web.Routing;
using Microsoft.AspNet.Identity;

namespace AuntieDot.Attributes {
    public class RequireSubscriptionAttribute : AuthorizeAttribute {
        public override void OnAuthorization(AuthorizationContext filterContext) {
            base.OnAuthorization(filterContext);
            //If default authorization failed, filterContext.Result will be set.
            //Ensure it's null before continuing.
            if (filterContext.Result == null) {
                var context = filterContext.HttpContext;
                var userId = context.User.Identity.GetUserId();
                //Get the shop's status from the CacheEngine.
                var status = CacheEngine.GetShopStatus(userId, context);
                if (status.BillingIsConnected && status.ShopIsConnected) {
                    //Assume subscription is valid.
                }
                else if (status.BillingIsConnected == false) {
                    //User has connected their Shopify shop, but they haven't accepted a subscription charge.
                    filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary {
                        {"controller", "Register"},
                        {"action", "Charge"}
                    });
                }
                else {
                    //User has created an account, but they haven't connected their Shopify shop.
                    filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary {
                        {"controller", "Register"},
                        {"action", "Connect"}
                    });
                }
            }
        }
    }
}