using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ADLRestaurant.Helpers;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace ADLRestaurant.Pages.Orders
{
    public class AddItemsModel : PageModel
    {
        private readonly IConfiguration _config;

        public AddItemsModel(IConfiguration config)
        {
            _config = config;
            string? connectionString = _config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or empty.");
            }
            DbHelper.Init(connectionString);
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
        public List<ItemModel> AvailableItems { get; set; } = new();
        public List<OrderItemModel> OrderItems { get; set; } = new();
        public decimal GrandTotal { get; set; }

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
            OrderId = id;

            if (SelectedItemId > 0 && Quantity > 0)
            {
                var reader = DbHelper.ExecuteReader("sp_GetItems", null);
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
                        { "@ClientId", 1 },
                        { "@UserId", 1 }
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
            var reader = DbHelper.ExecuteReader("sp_GetItems", null);
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
            var parameters = new Dictionary<string, object>
            {
                { "@OrderId", OrderId }
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

    }
}

