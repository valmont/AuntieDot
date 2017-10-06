using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace AuntieDot {
    public static class ApplicationEngine {
        public static string ShopifySecretKey => ConfigurationManager.AppSettings.Get("Shopify_Secret_Key");
        public static string ShopifyApiKey => ConfigurationManager.AppSettings.Get("Shopify_API_Key");

    }
}