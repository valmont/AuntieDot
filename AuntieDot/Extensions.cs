using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.Entity.Infrastructure;
using System.Linq;
using AuntieDot.Models;
using Microsoft.Extensions.Primitives;

namespace AuntieDot {
    public static class Extensions {
        public static List<KeyValuePair<string, StringValues>> ToKvps(this NameValueCollection qs) {
            var parameters = qs.Keys.Cast<string>().ToDictionary(key => key, value => qs[value]);
            var kvps = new List<KeyValuePair<string, StringValues>>();

            parameters.ToList().ForEach(x => {
                kvps.Add(new KeyValuePair<string, StringValues>(x.Key, new StringValues(x.Value)));
            });

            return kvps;
        }

        public static Order ToDatabaseOrder(this ShopifySharp.Order shopifyOrder) {
            if (shopifyOrder.Id == null) throw new InvalidOperationException("Order Id cannot be null");
            //Create a summary of the order's line items
            var firstItem = shopifyOrder.LineItems.FirstOrDefault();
            var summary = shopifyOrder.LineItems.Count() > 1
                ? firstItem?.Title + $" and {shopifyOrder.LineItems.Count() - 1} other items."
                : firstItem?.Title;
            //Build an order that we can store in our database.
            var order = new Order {
                ShopifyId = shopifyOrder.Id.Value,
                DisplayId = shopifyOrder.Name,
                LineItemSummary = summary,
                CustomerName = shopifyOrder.Customer.FirstName + " " + shopifyOrder.Customer.LastName,
                DateCreated = shopifyOrder.CreatedAt,
                IsOpen = shopifyOrder.ClosedAt == null
            };
            return order;
        }

        /// <summary>
        ///     Gets a reference to the user's <see cref="ApplicationUser.Orders" /> collection in the database,
        ///     without querying the entire collection from the database. Note: you can add new orders to this
        ///     collection with `ref.CurrentValue.Add()`; however, the current value WILL ALWAYS BE EMPTY.Use
        ///     `ref.Query()` to query and filter collection values from the database.
        /// </summary>
        public static DbCollectionEntry<ApplicationUser, Order>GetUserOrdersReference(this ApplicationDbContext db, string userId) {
            var userRef = db.Users.Attach(new ApplicationUser {Id = userId});
            var ordersRef = db.Entry(userRef).Collection(u => u.Orders);
            return ordersRef;
        }
    }
}