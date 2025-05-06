using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.SqlClient;
using System.Data;
using Microsoft.AspNetCore.Http;
using ADLRestaurant.Helpers;

namespace ADLRestaurant.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IConfiguration _config;

        public LoginModel(IConfiguration config)
        {
            _config = config;
            DbHelper.Init(_config.GetConnectionString("DefaultConnection"));
        }

        [BindProperty]
        public LoginInput Input { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public class LoginInput
        {
            public string UserId { get; set; } = "";
            public string LoginPin { get; set; } = "";
        }

        public IActionResult OnPost()
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "@UserId", Input.UserId },
                    { "@Pin", Input.LoginPin }
                };

                using var reader = DbHelper.ExecuteReader("sp_ValidateUserLogin", parameters);
                if (reader.Read())
                {
                    // Store login session
                    HttpContext.Session.SetString("UserId", Input.UserId);
                    HttpContext.Session.SetString("RestaurantId", reader["rid"].ToString());

                    return RedirectToPage("/Orders/Ongoing");
                }
                else
                {
                    ErrorMessage = "Invalid User ID or PIN.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error: {ex.Message}";
            }

            return Page();
        }
    }
}
