using ADLRestaurant.Helpers;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ADLRestaurant.Pages.Items
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _config;

        public IndexModel(IConfiguration config)
        {
            _config = config;
            DbHelper.Init(_config.GetConnectionString("DefaultConnection"));
        }

        public class ItemModel
        {
            public int ItemId { get; set; }
            public string ItemName { get; set; }
            public decimal Price { get; set; }
            public decimal GST { get; set; }
        }
        [BindProperty]
        public string ItemId { get; set; }
        [BindProperty]
        public string NewItemName { get; set; }

        [BindProperty]
        public decimal NewPrice { get; set; }

        [BindProperty]
        public decimal NewGST { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 5;
        public int TotalPages { get; set; }
        public List<ItemModel> ItemList { get; set; } = new();

        public void OnGet()
        {
            LoadItems();
        }

        public IActionResult OnPost()
        {
            if (!string.IsNullOrWhiteSpace(NewItemName))
            {
                var parameters = new Dictionary<string, object>
                {
                    { "@ItemName", NewItemName },
                    { "@Price", NewPrice },
                    { "@GST", NewGST },
                    { "@ClientId", 1 },
                    { "@UserId", 1 }
                };

                DbHelper.ExecuteNonQuery("sp_InsertItem", parameters);
            }

            return RedirectToPage(new { SearchTerm, PageNumber });
        }

        public IActionResult OnPostEdit()
        {
            
                if (NewItemName != null && NewPrice >= 0 && NewGST >= 0)
                {
                    var parameters = new Dictionary<string, object>
                    {
                        { "@ItemId", ItemId }, // Ensure this is set in the Edit modal
                        { "@ItemName", NewItemName },
                        { "@Price", NewPrice },
                        { "@GST", NewGST }
                    };

                    DbHelper.ExecuteNonQuery("sp_UpdateItem", parameters);
                }
            

            return RedirectToPage(new { SearchTerm, PageNumber });
        }

        public IActionResult OnPostDelete(int id)
        {
            DbHelper.ExecuteNonQuery("sp_DeleteItem", new Dictionary<string, object> { { "@ItemId", id } });
            return RedirectToPage(new { SearchTerm, PageNumber });
        }

        private void LoadItems()
        {
            var reader = DbHelper.ExecuteReader("sp_GetItems", null);
            var allItems = new List<ItemModel>();

            using (reader)
            {
                while (reader.Read())
                {
                    allItems.Add(new ItemModel
                    {
                        ItemId = Convert.ToInt32(reader["ItemId"]),
                        ItemName = reader["ItemName"].ToString(),
                        Price = Convert.ToDecimal(reader["Price"]),
                        GST = Convert.ToDecimal(reader["GST"])
                    });
                }
            }

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                allItems = allItems
                    .Where(i => i.ItemName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            int totalCount = allItems.Count;
            TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

            ItemList = allItems
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        }
    }
}
