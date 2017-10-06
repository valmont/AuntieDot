﻿namespace AuntieDot.Models {
    public class CachedShopStatus {
        public string Username { get; set; }

        /// <summary>
        ///     The user's ShopifyAccessToken, received from Shopify's OAuth integration flow.
        /// </summary>
        public string ShopifyAccessToken { get; set; }

        /// <summary>
        ///     The user's *.myshopify.com domain.
        /// </summary>
        public string MyShopifyDomain { get; set; }

        /// <summary>
        ///     The id of the user's Shopify subscription charge.
        /// </summary>
        public long? ShopifyChargeId { get; set; }

        public bool ShopIsConnected => string.IsNullOrEmpty(ShopifyAccessToken) == false;

        public bool BillingIsConnected => ShopifyChargeId.HasValue;
    }
}