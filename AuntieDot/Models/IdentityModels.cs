using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace AuntieDot.Models {
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit https://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser {
        public string ShopifyAccessToken { get; set; }
        public string MyShopifyDomain { get; set; }
        public long? ShopifyChargeId { get; set; }
        public DateTimeOffset? BillingOn { get; set; }
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public string WidgetTitle { get; set; }
        public string WidgetBlurb { get; set; }
        public string WidgetHexColor { get; set; }
        public long? ScriptTagId { get; set; }
        public Collection<Quiz> Quizzes { get; set; } = new Collection<Quiz>();

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager) {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser> {
        public ApplicationDbContext() : base("DefaultConnection", false) {
        }
        public DbSet<Quiz> Quizzes { get; set; }

        public static ApplicationDbContext Create() {
            return new ApplicationDbContext();
        }
    }
}