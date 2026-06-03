using System.Text;
using ClosedXML.Excel;
using BritishAgro.Inventory.Services.Inventory;

namespace BritishAgro.Inventory.Services.Export;

public interface IReportExportService
{
    byte[] ExportToCsv(MonthlyReportData data);
    byte[] ExportToExcel(MonthlyReportData data);
}

public sealed class ReportExportService : IReportExportService
{
    public byte[] ExportToCsv(MonthlyReportData data)
    {
        var sb = new StringBuilder();

        // Add header
        sb.AppendLine($"Monthly Stock Report - {data.Year:0000}-{data.Month:00}");
        sb.AppendLine();
        sb.AppendLine("Date,Product,Unit,Opening Stock,Received,Issued,Closing Stock");

        // Add data rows
        foreach (var item in data.ReportItems)
        {
            var dateStr = item.Date.ToString("yyyy-MM-dd");
            var openingStock = item.OpeningStock.ToString("0.##");
            var received = item.Received.ToString("0.##");
            var issued = item.Issued.ToString("0.##");
            var closingStock = item.ClosingStock.ToString("0.##");

            sb.AppendLine($"\"{dateStr}\",\"{item.ProductName}\",\"{item.UnitOfMeasurement}\",{openingStock},{received},{issued},{closingStock}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public byte[] ExportToExcel(MonthlyReportData data)
    {
        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Stock Report");

            // Set column widths
            worksheet.Column(1).Width = 12;
            worksheet.Column(2).Width = 25;
            worksheet.Column(3).Width = 10;
            worksheet.Column(4).Width = 15;
            worksheet.Column(5).Width = 15;
            worksheet.Column(6).Width = 15;
            worksheet.Column(7).Width = 15;

            // Add title
            var titleRow = worksheet.Row(1);
            titleRow.Cell(1).Value = $"Monthly Stock Report - {data.Year:0000}-{data.Month:00}";
            titleRow.Cell(1).Style.Font.Bold = true;
            titleRow.Cell(1).Style.Font.FontSize = 14;
            worksheet.Range("A1:G1").Merge();

            // Add empty row
            worksheet.Row(2).Height = 2;

            // Add headers
            var headerRow = worksheet.Row(3);
            var headers = new[] { "Date", "Product", "Unit", "Opening Stock", "Received", "Issued", "Closing Stock" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = headerRow.Cell(i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Add data rows
            int rowNum = 4;
            foreach (var item in data.ReportItems)
            {
                var row = worksheet.Row(rowNum);
                row.Cell(1).Value = item.Date;
                row.Cell(1).Style.NumberFormat.Format = "yyyy-mm-dd";
                row.Cell(2).Value = item.ProductName;
                row.Cell(3).Value = item.UnitOfMeasurement;
                row.Cell(4).Value = (double)item.OpeningStock;
                row.Cell(4).Style.NumberFormat.Format = "0.00";
                row.Cell(5).Value = (double)item.Received;
                row.Cell(5).Style.NumberFormat.Format = "0.00";
                row.Cell(6).Value = (double)item.Issued;
                row.Cell(6).Style.NumberFormat.Format = "0.00";
                row.Cell(7).Value = (double)item.ClosingStock;
                row.Cell(7).Style.NumberFormat.Format = "0.00";

                // Alternate row colors
                if (rowNum % 2 == 0)
                {
                    row.Style.Fill.BackgroundColor = XLColor.FromHtml("#E7E6E6");
                }

                rowNum++;
            }

            // Add summary section
            if (data.ReportItems.Count > 0)
            {
                rowNum += 2;
                var summaryRow = worksheet.Row(rowNum);
                summaryRow.Cell(1).Value = "Summary";
                summaryRow.Cell(1).Style.Font.Bold = true;

                // Summary rows
                rowNum++;
                var totalReceivedRow = worksheet.Row(rowNum);
                totalReceivedRow.Cell(1).Value = "Total Received:";
                totalReceivedRow.Cell(5).Value = (double)data.ReportItems.Sum(x => x.Received);
                totalReceivedRow.Cell(5).Style.NumberFormat.Format = "0.00";
                totalReceivedRow.Cell(5).Style.Font.Bold = true;

                rowNum++;
                var totalIssuedRow = worksheet.Row(rowNum);
                totalIssuedRow.Cell(1).Value = "Total Issued:";
                totalIssuedRow.Cell(6).Value = (double)data.ReportItems.Sum(x => x.Issued);
                totalIssuedRow.Cell(6).Style.NumberFormat.Format = "0.00";
                totalIssuedRow.Cell(6).Style.Font.Bold = true;
            }

            using (var ms = new MemoryStream())
            {
                workbook.SaveAs(ms);
                return ms.ToArray();
            }
        }
    }
}
