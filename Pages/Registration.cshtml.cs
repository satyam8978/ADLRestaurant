using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ADLRestaurant.Helpers;
using System.Data;
using System.Data.SqlClient;

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
            public string RestaurantName { get; set; } = "ADL Restaurant";
            public string OwnerName { get; set; } = "Satyan";
            public string Email { get; set; } = "satyam0297@gmail.com";
            public string MobileNumber { get; set; } = "8978970939";
        }

        public IActionResult OnPost()
        {
            SqlDataReader? reader = null;
            SqlDataReader? pinReader = null;

            try
            {
                // Step 1: Register Restaurant
                var parameters = new Dictionary<string, object>
                {
                    { "@RestaurantName", Input.RestaurantName },
                    { "@OwnerName", Input.OwnerName },
                    { "@Email", Input.Email },
                    { "@MobileNumber", Input.MobileNumber }
                };

                reader = DbHelper.ExecuteReader("sp_InsertRestaurant", parameters);
                if (reader.Read())
                {
                    // Step 2: Read restaurant values
                    RestaurantUniqueId = reader["RestaurantUniqueId"].ToString();
                    int restaurantId = Convert.ToInt32(reader["RestaurantId"]);

                    reader.Close(); // Close previous reader before new command

                    // Step 3: Insert Owner into Users table and capture UID
                    string userUid = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();


                    var userParams = new Dictionary<string, object>
{
    { "@RestaurantId", RestaurantUniqueId },
    { "@Name", Input.OwnerName },
    { "@MobileNumber", Input.MobileNumber },
    { "@Address", "N/A" },
    { "@Role", 1 },
    { "@Status", 1 },
    { "@CreatedBy", 0 }
  
};

                    var userReader = DbHelper.ExecuteReader("InsertUser", userParams);
                    string userId = userUid; // fallback
                    if (userReader.Read())
                    {
                        userId = userReader["Uid"].ToString();
                    }
                    userReader.Close();

                    // Step 4: Generate PIN
                    pinReader = DbHelper.ExecuteReader("GenerateLoginPinForUser", new Dictionary<string, object>
                    {
                        { "@UserId", userId }
                    });

                    string? loginPin = null;
                    if (pinReader.Read())
                    {
                        loginPin = pinReader["Pin"].ToString();
                    }

                    // Step 5: Send Email
                    _emailHelper.SendWelcomeEmail(Input.Email, RestaurantUniqueId, userId, loginPin);

                    SuccessMessage = "Registration successful. Login details sent to your email.";
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
            finally
            {
                reader?.Close();
                pinReader?.Close();
            }

            return Page();
        }
    }
}
