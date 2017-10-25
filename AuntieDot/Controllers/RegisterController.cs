﻿using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using AuntieDot.Models;
using Microsoft.AspNet.Identity.Owin;
using ShopifySharp;
using ShopifySharp.Enums;

namespace AuntieDot.Controllers {
    public class RegisterController : Controller {
        public ActionResult Index() {
            return View();
        }

        [Authorize]
        public ActionResult Connect() {
            //If the user came from the Shopify app store, we'll have stored their shop URL in acookie.
            //Try to pull in that cookie value so we can autofill the form on this page.
            var cookie = Request.Cookies.Get(".App.Handshake.ShopUrl");
            //Pass the cookie's value to the viewbag
            ViewBag.ShopUrl = cookie?.Value;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Index(RegisterViewModel model) {
            if (!ModelState.IsValid) return View(model);

            var owinContext = HttpContext.GetOwinContext();
            var userManager = owinContext.GetUserManager<ApplicationUserManager>();
            var user = new ApplicationUser {UserName = model.Email, Email = model.Email};
            //Try to create the new account
            var create = await userManager.CreateAsync(user, model.Password);
            if (create.Succeeded) {
                await owinContext.Get<ApplicationSignInManager>().SignInAsync(user, true, true
                );
                //User must now connect their Shopify shop
                return RedirectToAction("Connect");
            }
            foreach (var error in create.Errors) ModelState.AddModelError("", error);
            // If we got this far, something failed. Return the view with the same model to redis play form.
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Connect(string shopUrl) {
            if (!await AuthorizationService.IsValidShopDomainAsync(shopUrl)) {
                ModelState.AddModelError("", "The URL you entered is not a valid *.myshopify.comURL.");
                //Preserve the user's shopUrl so they don't have to type it in again.
                ViewBag.ShopUrl = shopUrl;
                return View();
            }
            //Determine the permissions that your app will need and request them here.
            var permissions = new List<AuthorizationScope> {
                AuthorizationScope.ReadProducts,
                AuthorizationScope.WriteProducts,
                AuthorizationScope.ReadContent,
                AuthorizationScope.WriteContent,
                AuthorizationScope.ReadOrders,
                AuthorizationScope.WriteOrders,
                AuthorizationScope.ReadScriptTags,
                AuthorizationScope.WriteScriptTags
            };

            //Prepare the redirect URL. This is case-sensitive and must match a 
            //redirection URL in your Shopify app's settings.
            var redirectUrl = "https://auntiedot.apphb.com/shopify/authresult";

            //Build the authorization URL
            var authUrl = AuthorizationService.BuildAuthorizationUrl(
                permissions,
                shopUrl,
                ApplicationEngine.ShopifyApiKey,
                redirectUrl);

            //Build the authorization URL and send the user to it
            return Redirect(authUrl.ToString());
        }

        [Authorize]
        public async Task<ActionResult> Charge() {
            //Grab the user object
            var owinContext = HttpContext.GetOwinContext();
            var usermanager = owinContext.GetUserManager<ApplicationUserManager>();
            var user = await usermanager.FindByNameAsync(User.Identity.Name);
            if (user == null) {
                return new HttpUnauthorizedResult();
            }
            var domain = user.MyShopifyDomain;
            var token = user.ShopifyAccessToken;
            //Build a test Shopify charge with a free, 14-day trial
            var charge = new RecurringCharge {
                Name = "Pro Plan",
                Price = 29.95m,
                TrialDays = 14,
                Test = true,
                ReturnUrl = "https://auntiedot.apphb.com/shopify/chargeresult"
            };
            //Create the charge
            charge = await new RecurringChargeService(domain, token).CreateAsync(charge);
            //Redirect the user to accept the charge
            return Redirect(charge.ConfirmationUrl);
        }
    }
}