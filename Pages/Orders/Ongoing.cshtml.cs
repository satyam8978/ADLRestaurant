using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ADLRestaurant.Helpers;
using System.Data;

namespace ADLRestaurant.Pages.Orders
{
    public class OngoingModel : PageModel
    {
        private readonly IConfiguration _config;
        
      

        public OngoingModel(IConfiguration config)
        {
            _config = config;
            DbHelper.Init(_config.GetConnectionString("DefaultConnection"));
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
                var parameters = new Dictionary<string, object>
                {
                    { "@TableId", NewOrder.TableId },
                    { "@TotalAmount", 0 },  // New order starts with 0 total
                    { "@ClientId", 1 },
                    { "@UserId", 1 }
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
            var reader = DbHelper.ExecuteReader("sp_GetOngoingOrders", null);
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
            var reader = DbHelper.ExecuteReader("sp_GetTables", null);
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
    }
}
