using ADLRestaurant.Helpers;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using static ADLRestaurant.Pages.Orders.AddItemsModel;

namespace ADLRestaurant.Pages.Orders
{
    public class OrderPrintModel : PageModel
    {
        private readonly IConfiguration _config;

        public int OrderId { get; set; }
        public int TableId { get; set; }
        public List<OrderItemModel> OrderItems { get; set; } = new();
        public decimal GrandTotal { get; set; }

        public OrderPrintModel(IConfiguration config)
        {
            _config = config;
            string? connectionString = _config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or empty.");
            }
            DbHelper.Init(connectionString);
        }

        public void OnGet(int orderId, int tableId)
        {
            OrderId = orderId;
            TableId = tableId;
            LoadOrderItems(orderId);
        }

        private void LoadOrderItems(int id)
        {
            var parameters = new Dictionary<string, object>
            {
                { "@OrderId", id }
            };

            var reader = DbHelper.ExecuteReader("sp_GetOrderItems", parameters);
            OrderItems.Clear();
            GrandTotal = 0;

            using (reader)
            {
                while (reader.Read())
                {
                    string itemName = reader["itemid"]?.ToString() ?? "Unknown";
                    decimal rate = reader["Rate"] != DBNull.Value ? (decimal)reader["Rate"] : 0m;
                    int quantity = reader["Quantity"] != DBNull.Value ? (int)reader["Quantity"] : 0;
                    decimal gst = reader["GST"] != DBNull.Value ? (decimal)reader["GST"] : 0m;
                    decimal amountWithoutGST = Math.Round(rate * quantity, 2);
                    decimal gstAmount = Math.Round(amountWithoutGST * (gst / 100), 2);
                    decimal totalAmount = Math.Round(amountWithoutGST + gstAmount, 2);

                    OrderItems.Add(new OrderItemModel
                    {
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
