using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ADLRestaurant.Helpers;

namespace ADLRestaurant.Pages.Tables
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _config;

        public IndexModel(IConfiguration config)
        {
            _config = config;

            // Initialize DbHelper with connection string
            DbHelper.Init(_config.GetConnectionString("DefaultConnection"));
        }

        public class TableModel
        {
            public int TableId { get; set; }
            public string TableName { get; set; }
        }

        [BindProperty]
        public string NewTableName { get; set; }

        public List<TableModel> TableList { get; set; } = new List<TableModel>();

        // Page load - get all tables
        public void OnGet()
        {
            LoadTables();
        }

        // Insert new table
        public IActionResult OnPost()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(NewTableName))
                {
                    var parameters = new Dictionary<string, object>
                    {
                        { "@TableName", NewTableName },
                        { "@ClientId", 1 }, // Sample client/user IDs
                        { "@UserId", 1 }
                    };

                    DbHelper.ExecuteNonQuery("sp_InsertTable", parameters);
                }
            }
            catch (Exception ex)
            {
                // Log or handle the error (for now, just rethrow)
                throw new Exception("Error inserting table", ex);
            }

            return RedirectToPage();
        }

        // Delete table by ID
        public IActionResult OnPostDelete(int id)
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "@TableId", id }
                };

                DbHelper.ExecuteNonQuery("sp_DeleteTable", parameters);
            }
            catch (Exception ex)
            {
                throw new Exception("Error deleting table", ex);
            }

            return RedirectToPage();
        }

        // Load all tables from DB
        private void LoadTables()
        {
            try
            {
                var reader = DbHelper.ExecuteReader("sp_GetTables", null);

                using (reader)
                {
                    while (reader.Read())
                    {
                        TableList.Add(new TableModel
                        {
                            TableId = Convert.ToInt32(reader["TableId"]),
                            TableName = reader["TableName"].ToString()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error loading tables", ex);
            }
        }
    }
}
