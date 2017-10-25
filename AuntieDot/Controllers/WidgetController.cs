using System;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Web.Mvc;
using AuntieDot.Models;
using Newtonsoft.Json;

namespace AuntieDot.Controllers {
    public class WidgetController : Controller {
        public async Task<ContentResult> Settings(string shop, string jsonp) {
            //Grab the user model that corresponds to this shop            
            using (var dbContext = new ApplicationDbContext()) {
                var user = await dbContext.Users.FirstOrDefaultAsync(u =>
                    u.MyShopifyDomain.Equals(shop, StringComparison.OrdinalIgnoreCase));
                if (user == null) throw new UnauthorizedAccessException("Shop does not exist in databas e.");
                //Encode the settings as JSON                
                var json = JsonConvert.SerializeObject(new {
                    Title = user.WidgetTitle,
                    Blurb = user.WidgetBlurb,
                    HexColor = user.WidgetHexColor
                });
                var outputScript = $"window['{jsonp}']({json})";
                //Content-Type must be text/javascript, or the JSONP request won't work.               
                return Content(outputScript, "text/javascript");
            }
        }

        public ContentResult Save(string firstName, string emailAddress, string shop, string jsonp) {
            // TODO: Figure out what you want to do with the email address.
            //Encode a generic result as JSON    
            string json = JsonConvert.SerializeObject(new {
                Success = true
            });
            string outputScript = $"window['{jsonp}']({json})";
            return Content(outputScript, "text/javascript");
        }
    }
}