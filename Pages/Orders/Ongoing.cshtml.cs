using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ADLRestaurant.Helpers;
using System.Data;


namespace ADLRestaurant.Pages.Orders
{
    public class OngoingModel : UserDetails
    {
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;


        public OngoingModel(IConfiguration config, IHttpContextAccessor httpContextAccessor)
        {
            _config = config;
            DbHelper.Init(_config.GetConnectionString("DefaultConnection"));
            _httpContextAccessor = httpContextAccessor;
        }

        public class OrderModel
        {
            public int OrderId { get; set; }
            public string tableid { get; set; }
            public string TableName { get; set; }
            public decimal TotalAmount { get; set; }
            public DateTime CreatedDate { get; set; }
           
        }

        public class TableModel
        {
            public int TableId { get; set; }
            public string TableName { get; set; }
        }

        public List<OrderModel> OngoingOrders { get; set; } = new();
        public List<TableModel> AvailableTables { get; set; } = new();

        [BindProperty]
        public TableModel NewOrder { get; set; } = new();

        public void OnGet()
        {
            LoadOngoingOrders();
            LoadAvailableTables();
        }

        public IActionResult OnPostCreateOrder()
        {
           
            if (NewOrder.TableId > 0)
            {
                var userId = _httpContextAccessor.HttpContext?.Session.GetString("UserId"); // Get UserId from session
                var rid = _httpContextAccessor.HttpContext?.Session.GetString("RestaurantId"); // Get RestaurantId from session

                var parameters = new Dictionary<string, object>
                {
                    { "@rid", rid },
                    { "@UserId", userId },
                    { "@TableId", NewOrder.TableId },
                };
                DbHelper.ExecuteNonQuery("sp_InsertOrder", parameters);
            }

            return RedirectToPage();
        }

        public IActionResult OnPostComplete(int id)
        {
            var parameters = new Dictionary<string, object>
            {
                { "@OrderId", id }
            };

            DbHelper.ExecuteNonQuery("sp_CompleteOrder", parameters);

            return RedirectToPage();
        }

        private void LoadOngoingOrders()
        {
            var rid = _httpContextAccessor.HttpContext?.Session.GetString("RestaurantId");
            var parameters = new Dictionary<string, object>
                {
                    { "@rid", rid },
                   
                };
            var reader = DbHelper.ExecuteReader("sp_GetOngoingOrders", parameters);
            using (reader)
            {
                while (reader.Read())
                {
                    OngoingOrders.Add(new OrderModel
                    {
                        tableid =  reader["tableid"].ToString(),
                        OrderId = Convert.ToInt32(reader["OrderId"]),
                        TableName = reader["TableName"].ToString(),
                        TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                    });
                }
            }
        }

        private void LoadAvailableTables()
        {
            LoadUserDetails();
            var parameters = new Dictionary<string, object>
                {

                    { "@ClientId",clientid },
                };
            var reader = DbHelper.ExecuteReader("sp_GetTables", parameters);
            using (reader)
            {
                while (reader.Read())
                {
                    AvailableTables.Add(new TableModel
                    {
                        TableId = Convert.ToInt32(reader["TableId"]),
                        TableName = reader["TableName"].ToString()
                    });
                }
            }
        }

        private string GetTableName(object tableId)
        {
            var id = Convert.ToInt32(tableId);
            var table = AvailableTables.FirstOrDefault(t => t.TableId == id);
            return table?.TableName ?? "Unknown";
        }
        public IActionResult OnPostGoToAddItems(int orderId, string tableId)
        {
            _httpContextAccessor.HttpContext?.Session.SetInt32("OrderId", orderId);
            _httpContextAccessor.HttpContext?.Session.SetString("TableId", tableId);

            return RedirectToPage("/Orders/AddItems");
        }

    }
}
