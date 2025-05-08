using ADLRestaurant.Helpers;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using static ADLRestaurant.Pages.Orders.AddItemsModel;
using iTextSharp.text;
using iTextSharp.text.pdf;
namespace ADLRestaurant.Pages.Orders
{
    public class OrderPrintModel : PageModel
    {
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public int OrderId { get; set; }
        public int TableId { get; set; }
        public List<OrderItemModel> OrderItems { get; set; } = new();
        public decimal GrandTotal { get; set; }

        public OrderPrintModel(IConfiguration config, IHttpContextAccessor httpContextAccessor)
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

        public IActionResult OnGetDownloadPdf(int orderIds)
        {
            OrderId = orderIds;

            // Load your order items here from DB or any service
            LoadOrderItems(orderIds);

            using var ms = new MemoryStream();
            var doc = new Document(PageSize.A4, 10, 10, 10, 10);
            var writer = PdfWriter.GetInstance(doc, ms);
            writer.CloseStream = false;

            doc.Open();

            var headerFont = FontFactory.GetFont(FontFactory.COURIER_BOLD, 14);
            var subHeaderFont = FontFactory.GetFont(FontFactory.COURIER_BOLD, 12);
            var normalFont = FontFactory.GetFont(FontFactory.COURIER, 10);
            var boldFont = FontFactory.GetFont(FontFactory.COURIER_BOLD, 10);

            doc.Add(new Paragraph("ADL Restaurant", headerFont)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 6
            });

            doc.Add(new Paragraph("------------------------------", normalFont));
            doc.Add(new Paragraph($"Order #: {OrderId}", subHeaderFont)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 2
            });

            doc.Add(new Paragraph($"Date: {DateTime.Now:dd-MM-yyyy HH:mm}", normalFont));
            doc.Add(new Paragraph(" "));

            PdfPTable table = new(6)
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

            doc.Add(new Paragraph($"Grand Total: ₹ {GrandTotal}", boldFont)
            {
                Alignment = Element.ALIGN_RIGHT,
                SpacingBefore = 6
            });

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
        private void LoadOrderItems(int id)
        {
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
