using ADLRestaurant.Helpers;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using static ADLRestaurant.Pages.Orders.AddItemsModel;
using iTextSharp.text;
using iTextSharp.text.pdf;
namespace ADLRestaurant.Pages.Orders
{
    public class OrderSummaryModel : PageModel
    {
        private readonly IConfiguration _config;
        public int OrderId { get; set; }
        public int TableId { get; set; }
        public List<OrderItemModel> OrderItems { get; set; } = new();
        public decimal GrandTotal { get; set; }

        [BindProperty]
        public string PaymentMode { get; set; }

        public OrderSummaryModel(IConfiguration config)
        {
            _config = config;
            string? connectionString = _config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or empty.");
            }
            DbHelper.Init(connectionString);
        }

        public void OnGet(int id, int tableId)
        {
            OrderId = id;
            TableId = tableId;
            LoadOrderItems(OrderId); // Fetch the items for the order
        }

        public IActionResult OnPostSubmitOrder()
        {
            // Save payment mode and complete the order in the database
            var parameters = new Dictionary<string, object>
            {
                { "@OrderId", OrderId },
                { "@PaymentMode", PaymentMode }
            };

            DbHelper.ExecuteScalar("sp_CompleteOrderWithPayment", parameters); // Adjust stored procedure to handle payment mode
            return RedirectToPage("/Orders/Ongoing");
        }

        private void LoadOrderItems(int orderids)
        {
            var parameters = new Dictionary<string, object>
            {
                { "@OrderId", orderids }
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
        public IActionResult OnPostDownloadPdf(int orderIds)
        {
            int orderId = orderIds;
            int tableId = Convert.ToInt32(Request.Form["TableId"]);

            OrderId = orderId;
            TableId = tableId;

            LoadOrderItems(orderIds);

            using var ms = new MemoryStream();
            var doc = new Document(PageSize.A4, 10, 10, 10, 10); // Use A4 or any other page size you prefer
            var writer = PdfWriter.GetInstance(doc, ms);
            writer.CloseStream = false;

            doc.Open();

            // Fonts
            var headerFont = FontFactory.GetFont(FontFactory.COURIER_BOLD, 14);
            var subHeaderFont = FontFactory.GetFont(FontFactory.COURIER_BOLD, 12);
            var normalFont = FontFactory.GetFont(FontFactory.COURIER, 10);
            var boldFont = FontFactory.GetFont(FontFactory.COURIER_BOLD, 10);

            // Header
            doc.Add(new Paragraph("ADL Restaurant", headerFont)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 6
            });

            doc.Add(new Paragraph("------------------------------", normalFont));

            // Order Summary
            doc.Add(new Paragraph($"Order #: {OrderId}", subHeaderFont)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 2
            });

            doc.Add(new Paragraph($"Table #: {TableId}", normalFont));
            doc.Add(new Paragraph($"Date: {DateTime.Now:dd-MM-yyyy HH:mm}", normalFont));
            doc.Add(new Paragraph(" "));

            // Item Table: Item | Qty | Rate | GST | GST Amt | Total
            PdfPTable table = new PdfPTable(6)
            {
                WidthPercentage = 100
            };
            table.SetWidths(new float[] { 3, 1, 2, 1, 2, 2 });

            void AddCell(string text, Font font) =>
                table.AddCell(new PdfPCell(new Phrase(text, font)) { Border = Rectangle.NO_BORDER });

            AddCell("Item", boldFont);
            AddCell("Qty", boldFont);
            AddCell("Rate", boldFont);
            AddCell("GST%", boldFont);
            AddCell("GST Amt", boldFont);
            AddCell("Total", boldFont);

            foreach (var item in OrderItems)
            {
                AddCell(item.ItemName, normalFont);
                AddCell(item.Quantity.ToString(), normalFont);
                AddCell($"₹{item.Rate}", normalFont);
                AddCell($"{item.GST}%", normalFont);
                AddCell($"₹{item.GSTAmount}", normalFont);
                AddCell($"₹{item.TotalAmount}", normalFont);
            }

            doc.Add(table);
            doc.Add(new Paragraph(" "));

            // Total
            doc.Add(new Paragraph($"Grand Total: ₹ {GrandTotal}", boldFont)
            {
                Alignment = Element.ALIGN_RIGHT,
                SpacingBefore = 6
            });

            // Footer
            doc.Add(new Paragraph("------------------------------", normalFont));
            doc.Add(new Paragraph("Thank you! Visit Again!", normalFont)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingBefore = 6
            });

            doc.Close();
            ms.Position = 0;

            Response.Headers.Add("Content-Disposition", $"inline; filename=OrderReceipt_{OrderId}.pdf");
            return File(ms.ToArray(), "application/pdf");
        }




    }
}
