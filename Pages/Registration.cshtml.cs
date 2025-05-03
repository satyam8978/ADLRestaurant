using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ADLRestaurant.Helpers;
using System.Data;

namespace ADLRestaurant.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly IConfiguration _config;
        private readonly EmailHelper _emailHelper;
        public RegisterModel(IConfiguration config, EmailHelper emailHelper)
        {
            _config = config;
            DbHelper.Init(_config.GetConnectionString("DefaultConnection"));
            _emailHelper = emailHelper;
        }

        [BindProperty]
        public RestaurantInput Input { get; set; } = new();

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public string? GeneratedPassword { get; set; }
        public string? RestaurantUniqueId { get; set; }

        public class RestaurantInput
        {
            public string RestaurantName { get; set; } = "";
            public string OwnerName { get; set; } = "";
            public string Email { get; set; } = "";
            public string MobileNumber { get; set; } = "";
        }

        public IActionResult OnPost()
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "@RestaurantName", Input.RestaurantName },
                    { "@OwnerName", Input.OwnerName },
                    { "@Email", Input.Email },
                    { "@MobileNumber", Input.MobileNumber }
                };

                var reader = DbHelper.ExecuteReader("sp_InsertRestaurant", parameters);
                if (reader.Read())
                {
                    GeneratedPassword = reader["GeneratedPassword"].ToString();
                    RestaurantUniqueId = reader["RestaurantUniqueId"].ToString();
                    SuccessMessage = "Restaurant registered successfully.";
                    // Send welcome email
                    _emailHelper.SendWelcomeEmail(Input.Email, RestaurantUniqueId, GeneratedPassword);
                }
                else
                {
                    ErrorMessage = "Failed to register. Please try again.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error: " + ex.Message;
            }

            return Page();
        }
    }
}
