using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ADLRestaurant.Helpers
{
    public class UserDetails:PageModel
    {
        public string userid { get; set; }
        public string clientid { get; set; }
        public void LoadUserDetails()
        {
            userid = HttpContext.Session.GetString("UserId");
            clientid = HttpContext.Session.GetString("RestaurantId");
        }
    }
}
