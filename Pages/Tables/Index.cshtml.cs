using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ADLRestaurant.Helpers;

namespace ADLRestaurant.Pages.Tables
{
    public class IndexModel : UserDetails
    {
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public IndexModel(IConfiguration config, IHttpContextAccessor httpContextAccessor)
        {
            _config = config;
            DbHelper.Init(_config.GetConnectionString("DefaultConnection"));
            _httpContextAccessor = httpContextAccessor;
        }

        public class TableModel
        {
            public int TableId { get; set; }
            public string TableName { get; set; }
        }

        // Add Table
        [BindProperty]
        public string NewTableName { get; set; }

        // Table List
        public List<TableModel> TableList { get; set; } = new();

        // Search & Pagination
        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 5;
        public int TotalPages { get; set; }
        public int CurrentPage => PageNumber;
        public string PageName => "Index"; // For pagination link reuse

        public void OnGet()
        {
            LoadTables();
        }

        public IActionResult OnPost()
        {
            if (!string.IsNullOrWhiteSpace(NewTableName))
            {

                // Retrieve OrderId and UserId from session if already set
               
             LoadUserDetails();

                var parameters = new Dictionary<string, object>
                {
                    { "@TableName", NewTableName },
                    { "@ClientId",clientid },
                    { "@UserId", userid }
                };

                DbHelper.ExecuteNonQuery("sp_InsertTable", parameters);
            }

            return RedirectToPage(new { searchTerm = SearchTerm, pageNumber = PageNumber });
        }

        public IActionResult OnPostDelete(int id)
        {
            DbHelper.ExecuteNonQuery("sp_DeleteTable", new Dictionary<string, object> { { "@TableId", id } });
            return RedirectToPage(new { searchTerm = SearchTerm, pageNumber = PageNumber });
        }

        private void LoadTables()
        {
            LoadUserDetails();
            var parameters = new Dictionary<string, object>
                {
                   
                    { "@ClientId",clientid },
                };
            var reader = DbHelper.ExecuteReader("sp_GetTables", parameters);
            var allTables = new List<TableModel>();

            using (reader)
            {
                while (reader.Read())
                {
                    allTables.Add(new TableModel
                    {
                        TableId = Convert.ToInt32(reader["TableId"]),
                        TableName = reader["TableName"].ToString()
                    });
                }
            }

            // Search filter
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                allTables = allTables
                    .Where(t => t.TableName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Pagination
            int totalCount = allTables.Count;
            TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

            TableList = allTables
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        }
    }
}
