using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ADLRestaurant.Helpers;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using static iTextSharp.text.pdf.AcroFields;

namespace ADLRestaurant.Pages.Orders
{
    public class AddItemsModel : UserDetails
    {
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AddItemsModel(IConfiguration config, IHttpContextAccessor httpContextAccessor)
        {
            _config = config;
            string? connectionString = _config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or empty.");
            }
            DbHelper.Init(connectionString);
            _httpContextAccessor = httpContextAccessor;
        }

        public class ItemModel
        {
            public int ItemId { get; set; }
            public string ItemName { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public decimal GST { get; set; }
        }

        public class OrderItemModel
        {
            public int OrderItemId { get; set; }
            public string ItemName { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal Rate { get; set; }
            public decimal GST { get; set; }
            public decimal GSTAmount { get; set; }
            public decimal TotalAmount { get; set; }
        }
        [BindProperty]
        public int OrderId { get; set; }
        [BindProperty]
        public int TableId { get; set; }
        [BindProperty]
        public string PaymentMode { get; set; }

        [BindProperty]
        public decimal GrandTotal { get; set; }

        [BindProperty]
    

        public List<ItemModel> AvailableItems { get; set; } = new();
        public List<OrderItemModel> OrderItems { get; set; } = new();
      

        [BindProperty]
        public int SelectedItemId { get; set; }

        [BindProperty]
        public int Quantity { get; set; }

        public void OnGet(int id,int tableId)
        {
            OrderId = id;
            TableId = tableId;
            LoadAvailableItems(); // First
            LoadOrderItems();     // Then
        }

        public IActionResult OnPost(int id)
        {
            OrderId = _httpContextAccessor.HttpContext?.Session.GetInt32("OrderId") ?? 0;
            var rid = _httpContextAccessor.HttpContext?.Session.GetString("RestaurantId");
            OrderId = OrderId;
            LoadUserDetails();
            var parameterss = new Dictionary<string, object>
                {

                    { "@ClientId",clientid },
                };
            if (SelectedItemId > 0 && Quantity > 0)
            {
                var reader = DbHelper.ExecuteReader("sp_GetItems", parameterss);
                using (reader)
                {
                    while (reader.Read())
                    {
                        if (reader["ItemId"] != DBNull.Value && (int)reader["ItemId"] == SelectedItemId)
                        {
                            decimal rate = reader["Price"] != DBNull.Value ? Convert.ToDecimal(reader["Price"]) : 0m;
                            decimal gst = reader["GST"] != DBNull.Value ? Convert.ToDecimal(reader["GST"]) : 0m;

                            // Calculate amount without GST
                            decimal amountWithoutGST = rate * Quantity;

                            // Calculate GST amount and round it to 2 decimal places
                            decimal gstAmount = Math.Round(amountWithoutGST * (gst / 100), 2);

                            // Calculate total amount and round it to 2 decimal places
                            decimal totalAmount = Math.Round(amountWithoutGST + gstAmount, 2);

                            // Insert the order item into the database
                            var parameters = new Dictionary<string, object>
                    {
                        { "@OrderId", OrderId },
                        { "@ItemId", SelectedItemId },
                        { "@Quantity", Quantity },
                        { "@Rate", rate },
                        { "@GST", gst },
                        { "@Amount", totalAmount },
                        { "@ClientId", clientid },
                        { "@UserId",userid }
                    };

                            DbHelper.ExecuteScalar("sp_InsertOrderItem", parameters);
                            break;
                        }
                    }
                }
            }

            // Reload available items and order items after the operation
            LoadAvailableItems(); // First
            LoadOrderItems();     // Then
            return Page();
        }

        public IActionResult OnPostRemoveItem(int itemId)
        {
            var parameters = new Dictionary<string, object>
            {
                { "@OrderItemId", itemId }
            };

            DbHelper.ExecuteScalar("sp_RemoveOrderItem", parameters);

            LoadAvailableItems(); // First
            LoadOrderItems();     // Then
            return Page();
        }

        public IActionResult OnPostCompleteOrder()
        {
            // Insert any necessary logic to complete the order if needed
            // Example: Updating order status in the database or calculating the grand total

            var parameters = new Dictionary<string, object>
            {
                { "@OrderId", OrderId },
                { "@TableId", TableId }
            };

           // DbHelper.ExecuteScalar("sp_CompleteOrder", parameters); // You can adjust the stored procedure as per your needs

            // After completing the order, redirect to the order summary page
            return RedirectToPage("/Orders/OrderSummary", new { id = OrderId, tableId = TableId });
        }
        private void LoadAvailableItems()
        {
           LoadUserDetails();
            var parameters = new Dictionary<string, object>
            {
                        { "@clientid", clientid }, // Ensure this is set in the Edit modal
                        
                    };
            var reader = DbHelper.ExecuteReader("sp_GetItems", parameters);
            AvailableItems.Clear();

            using (reader)
            {
                while (reader.Read())
                {
                    AvailableItems.Add(new ItemModel
                    {
                        ItemId = reader["ItemId"] != DBNull.Value ? (int)reader["ItemId"] : 0,
                        ItemName = reader["ItemName"]?.ToString() ?? string.Empty,
                        Price = reader["Price"] != DBNull.Value ? (decimal)reader["Price"] : 0m,
                        GST = reader["GST"] != DBNull.Value ? (decimal)reader["GST"] : 0m
                    });
                }
            }
        }

        private void LoadOrderItems()
        {
            OrderId = _httpContextAccessor.HttpContext?.Session.GetInt32("OrderId") ?? 0;
            var rid = _httpContextAccessor.HttpContext?.Session.GetString("RestaurantId");
            var parameters = new Dictionary<string, object>
            {
                { "@OrderId", OrderId },
                   { "@Clientid", rid }
            };

            var reader = DbHelper.ExecuteReader("sp_GetOrderItems", parameters);
            OrderItems.Clear();
            GrandTotal = 0;

            using (reader)
            {
                while (reader.Read())
                {
                    int itemId = reader["ItemId"] != DBNull.Value ? (int)reader["ItemId"] : 0;

                    // Correctly get ItemName from AvailableItems
                    string itemName = AvailableItems.FirstOrDefault(x => x.ItemId == itemId)?.ItemName ?? "Unknown";

                    decimal rate = reader["Rate"] != DBNull.Value ? (decimal)reader["Rate"] : 0m;
                    int quantity = reader["Quantity"] != DBNull.Value ? (int)reader["Quantity"] : 0;
                    decimal gst = reader["GST"] != DBNull.Value ? (decimal)reader["GST"] : 0m;

                    // Round the rate, GST, and Total Amount to 2 decimal places
                    decimal amountWithoutGST = Math.Round(rate * quantity, 2); // Round the amount without GST
                    decimal gstAmount = Math.Round(amountWithoutGST * (gst / 100), 2); // Round GST amount
                    decimal totalAmount = Math.Round(amountWithoutGST + gstAmount, 2); // Round total amount including GST

                    OrderItems.Add(new OrderItemModel
                    {
                        OrderItemId = reader["OrderItemId"] != DBNull.Value ? (int)reader["OrderItemId"] : 0,
                        ItemName = itemName,
                        Quantity = quantity,
                        Rate = rate,
                        GST = gst,
                        GSTAmount = gstAmount,
                        TotalAmount = totalAmount
                    });

                    GrandTotal += totalAmount;
                }
            }
        }

        public IActionResult OnPostMakePayment()
        {
            var userId = _httpContextAccessor.HttpContext?.Session.GetString("UserId"); // Get UserId from session
            var rid = _httpContextAccessor.HttpContext?.Session.GetString("RestaurantId"); // Get RestaurantId from session
            var parameters = new Dictionary<string, object>
    {
        { "@OrderId", OrderId },
        { "@TotalAmount", GrandTotal },
        { "@PaymentMethod", PaymentMode },
        { "@PaidAmount", GrandTotal },  // assuming full paid for now
        { "@ClientId",rid },
        { "@UserId", userId },
        { "@CreatedDate", DateTime.Now }
    };

            var result = DbHelper.ExecuteReader("sp_CompleteOrderWithPayment", parameters);

            // Check if result is valid or payment succeeded
            bool paymentSuccess = result != null && result.HasRows;

            if (paymentSuccess)
            {
                TempData["PaymentMessage"] = "Payment successful! Redirecting to print page...";

                // Redirect to the PDF generation page (will open in a new tab by default)
                return RedirectToPage("/Orders/OrderPrint",new { handler = "DownloadPdf", orderIds = OrderId });
            }
            else
            {
                // Show failure message
                TempData["PaymentMessage"] = "Payment failed! Please try again.";
                // Return to the current page to show the error
                return RedirectToPage("/Orders/AddItems", new { id = OrderId, tableId = TableId });
            }
        }


    }
}

