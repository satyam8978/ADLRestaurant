using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using ADLRestaurant.Helpers;

using System.IO;
using System;
using System.Collections.Generic;
using System.Text;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.tool.xml;

namespace ADLRestaurant.Pages.Orders
{
    public class CompletedModel : PageModel
    {
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CompletedModel(IConfiguration config, IHttpContextAccessor httpContextAccessor)
        {
            _config = config;
            _httpContextAccessor = httpContextAccessor;
            DbHelper.Init(_config.GetConnectionString("DefaultConnection"));
        }

        public class OrderModel
        {
            public int OrderId { get; set; }
            public string TableName { get; set; }
            public decimal TotalAmount { get; set; }
            public DateTime CompletedDate { get; set; }
        }

        public List<OrderModel> CompletedOrders { get; set; } = new();
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public DateTime? SearchDate { get; set; }

        public void OnGet(int page = 1)
        {
            CurrentPage = page;

            if (!SearchDate.HasValue)
            {
                SearchDate = DateTime.Today;
            }

            string rid = _httpContextAccessor.HttpContext?.Session.GetString("RestaurantId");
            var parameters = new Dictionary<string, object>
            {
                { "@rid", rid },
                { "@SearchDate", SearchDate?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value },
                { "@Offset", (page - 1) * 15 },
                { "@Limit", 15 }
            };

            using var reader = DbHelper.ExecuteReader("sp_GetCompletedOrdersPaginated", parameters);
            CompletedOrders.Clear();
            while (reader.Read())
            {
                CompletedOrders.Add(new OrderModel
                {
                    OrderId = Convert.ToInt32(reader["OrderId"]),
                    TableName = reader["TableName"].ToString(),
                    TotalAmount = reader["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(reader["TotalAmount"]) : 0,
                    CompletedDate = Convert.ToDateTime(reader["CompletedDate"])
                });
            }

            var totalRecordsObj = DbHelper.ExecuteScalar("sp_GetCompletedOrdersCount", new Dictionary<string, object>
            {
                { "@rid", rid },
                { "@SearchDate", SearchDate?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value }
            });

            int totalRecords = Convert.ToInt32(totalRecordsObj);
            TotalPages = (int)Math.Ceiling(totalRecords / 15.0);
        }



        public IActionResult OnGetExportToPdf()
        {
            string rid = _httpContextAccessor.HttpContext?.Session.GetString("RestaurantId");
            if (string.IsNullOrEmpty(rid))
            {
                return RedirectToPage(); // fallback
            }

            string searchDateStr = SearchDate?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd");

            var parameters = new Dictionary<string, object>
    {
        { "@rid", rid },
        { "@SearchDate", searchDateStr }
    };

            List<OrderModel> orders = new();
            using var reader = DbHelper.ExecuteReader("sp_GetCompletedOrders", parameters);
            while (reader.Read())
            {
                orders.Add(new OrderModel
                {
                    OrderId = Convert.ToInt32(reader["OrderId"]),
                    TableName = reader["TableId"].ToString(),
                    TotalAmount = reader["Amount"] != DBNull.Value ? Convert.ToDecimal(reader["Amount"]) : 0,
                    CompletedDate = Convert.ToDateTime(reader["CompletedDate"])
                });
            }

            // 🔶 Build HTML
            var sb = new StringBuilder();
            sb.Append(@"
    <html>
    <head>
    <style>
        body { font-family: Arial; font-size: 12px; }
        table { width: 100%; border-collapse: collapse; margin-top: 20px; }
        th, td { border: 1px solid #000; padding: 8px; text-align: left; }
        .header { text-align: center; }
        .footer { 
            text-align: center; 
            font-size: 9px; 
            color: gray; 
            margin-top: 20px;
        }
    </style>
    </head>
    <body>
        <div class='header'>
            <h2>SR Restaurant</h2>
            <p>GST No: 1234567890 | Address: Hyderabad, Telangana</p>
            <h3>Completed Orders - " + searchDateStr + @"</h3>
        </div>
        <table>
            <thead>
                <tr>
                    <th>Order ID</th>
                    <th>Table Name</th>
                    <th>Total</th>
                    <th>Completed Date</th>
                </tr>
            </thead>
            <tbody>");
            foreach (var order in orders)
            {
                sb.AppendFormat(@"
        <tr>
            <td>{0}</td>
            <td>{1}</td>
            <td>₹{2}</td>
            <td>{3}</td>
        </tr>",
                    order.OrderId,
                    order.TableName,
                    order.TotalAmount.ToString("0.00"),
                    order.CompletedDate.ToString("dd-MMM-yyyy hh:mm tt")
                );
            }
            sb.Append(@"
            </tbody>
        </table>");

            // Add footer directly to the HTML
            sb.Append(@"
        <div class='footer'>
            Designed and developed by ADLSoftTCodes
        </div>
    </body>
    </html>");

            // 🔶 Convert to PDF
            using var stream = new MemoryStream();
            var document = new Document(PageSize.A4, 20, 20, 20, 20);
            var writer = PdfWriter.GetInstance(document, stream);
            document.Open();

            using (var sr = new StringReader(sb.ToString()))
            {
                XMLWorkerHelper.GetInstance().ParseXHtml(writer, document, sr);
            }

            document.Close();

            return File(stream.ToArray(), "application/pdf", $"CompletedOrders_{searchDateStr}.pdf");
        }




    }
}
