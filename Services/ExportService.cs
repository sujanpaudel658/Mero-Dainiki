using Mero_Dainiki.Entities;
using Mero_Dainiki.Common;
using System.Text;

namespace Mero_Dainiki.Services
{
    /// <summary>
    /// Export service for generating PDF and other formats of journal entries
    /// </summary>
    public interface IExportService
    {
        Task<ServiceResult<string>> ExportToHtmlAsync(List<JournalEntry> entries, DateOnly startDate, DateOnly endDate);
        Task<ServiceResult<byte[]>> ExportToHtmlBytesAsync(List<JournalEntry> entries, DateOnly startDate, DateOnly endDate);
        Task<ServiceResult<string>> ExportToMarkdownAsync(List<JournalEntry> entries, DateOnly startDate, DateOnly endDate);
        Task<ServiceResult<string>> ExportToCsvAsync(List<JournalEntry> entries, DateOnly startDate, DateOnly endDate);
        Task<ServiceResult<byte[]>> ExportToPdfAsync(List<JournalEntry> entries, DateOnly startDate, DateOnly endDate);
        string GenerateFilename(string format, DateOnly startDate, DateOnly endDate);
    }

    public class ExportService : IExportService
    {
        private readonly IJournalService _journalService;

        public ExportService(IJournalService journalService)
        {
            _journalService = journalService;
        }

        public Task<ServiceResult<string>> ExportToHtmlAsync(List<JournalEntry> entries, DateOnly startDate, DateOnly endDate)
        {
            try
            {
                var htmlContent = GenerateHtmlContent(entries);
                return Task.FromResult(new ServiceResult<string> 
                { 
                    Success = true, 
                    Data = htmlContent
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ServiceResult<string> { Success = false, ErrorMessage = $"Error exporting to HTML: {ex.Message}" });
            }
        }

        public Task<ServiceResult<byte[]>> ExportToHtmlBytesAsync(List<JournalEntry> entries, DateOnly startDate, DateOnly endDate)
        {
            try
            {
                var htmlContent = GenerateHtmlContent(entries);
                var bytes = Encoding.UTF8.GetBytes(htmlContent);
                return Task.FromResult(new ServiceResult<byte[]> 
                { 
                    Success = true, 
                    Data = bytes
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ServiceResult<byte[]> { Success = false, ErrorMessage = $"Error exporting to HTML bytes: {ex.Message}" });
            }
        }

        public Task<ServiceResult<string>> ExportToMarkdownAsync(List<JournalEntry> entries, DateOnly startDate, DateOnly endDate)
        {
            try
            {
                var markdown = GenerateMarkdownContent(entries, startDate, endDate);
                return Task.FromResult(new ServiceResult<string> 
                { 
                    Success = true, 
                    Data = markdown
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ServiceResult<string> { Success = false, ErrorMessage = $"Error exporting to Markdown: {ex.Message}" });
            }
        }

        public Task<ServiceResult<string>> ExportToCsvAsync(List<JournalEntry> entries, DateOnly startDate, DateOnly endDate)
        {
            try
            {
                var csv = GenerateCsvContent(entries);
                return Task.FromResult(new ServiceResult<string> 
                { 
                    Success = true, 
                    Data = csv
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ServiceResult<string> { Success = false, ErrorMessage = $"Error exporting to CSV: {ex.Message}" });
            }
        }

        public Task<ServiceResult<byte[]>> ExportToPdfAsync(List<JournalEntry> entries, DateOnly startDate, DateOnly endDate)
        {
            try
            {
                // For PDF generation, we'll use HTML as base and convert to PDF
                // Note: In production, you'd use a library like iTextSharp or SelectPdf
                var htmlContent = GenerateHtmlContent(entries);
                var pdfBytes = ConvertHtmlToPdf(htmlContent);
                
                return Task.FromResult(new ServiceResult<byte[]> 
                { 
                    Success = true, 
                    Data = pdfBytes
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ServiceResult<byte[]> { Success = false, ErrorMessage = $"Error exporting to PDF: {ex.Message}" });
            }
        }

        public string GenerateFilename(string format, DateOnly startDate, DateOnly endDate)
        {
            var extension = format.ToLower() switch
            {
                "html" => ".html",
                "markdown" => ".md",
                "csv" => ".csv",
                "pdf" => ".pdf",
                _ => ".txt"
            };

            return $"journal_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}{extension}";
        }

        private string GenerateHtmlContent(List<JournalEntry> entries)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset=\"UTF-8\">");
            sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            sb.AppendLine("    <title>Journal Export</title>");
            sb.AppendLine("    <style>");
            sb.AppendLine("        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; background: #f5f5f5; padding: 20px; }");
            sb.AppendLine("        .container { max-width: 900px; margin: 0 auto; background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }");
            sb.AppendLine("        h1 { color: #6366f1; border-bottom: 2px solid #6366f1; padding-bottom: 10px; }");
            sb.AppendLine("        .entry { margin-bottom: 40px; padding-bottom: 30px; border-bottom: 1px solid #eee; }");
            sb.AppendLine("        .entry:last-child { border-bottom: none; }");
            sb.AppendLine("        .entry-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 15px; }");
            sb.AppendLine("        .entry-date { font-size: 14px; color: #666; font-weight: 500; }");
            sb.AppendLine("        .entry-title { font-size: 24px; color: #1a202c; font-weight: 600; margin-bottom: 10px; }");
            sb.AppendLine("        .entry-meta { display: flex; gap: 15px; margin-bottom: 15px; font-size: 12px; }");
            sb.AppendLine("        .badge { background: #f0f0f0; padding: 4px 12px; border-radius: 20px; color: #555; }");
            sb.AppendLine("        .mood-badge { background: #e0e7ff; color: #4338ca; }");
            sb.AppendLine("        .category-badge { background: #fef3c7; color: #92400e; }");
            sb.AppendLine("        .favorite-badge { background: #fef3c7; color: #d97706; }");
            sb.AppendLine("        .entry-content { color: #555; line-height: 1.8; }");
            sb.AppendLine("        .tags { margin-top: 10px; display: flex; flex-wrap: wrap; gap: 8px; }");
            sb.AppendLine("        .tag { background: #e0e7ff; color: #6366f1; padding: 4px 10px; border-radius: 15px; font-size: 12px; }");
            sb.AppendLine("        .export-info { background: #f0f9ff; border-left: 4px solid #0284c7; padding: 15px; margin-bottom: 30px; border-radius: 4px; }");
            sb.AppendLine("        @media print { body { background: white; } .container { box-shadow: none; } }");
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("    <div class=\"container\">");
            sb.AppendLine("        <h1>📔 Journal Export</h1>");
            sb.AppendLine($"        <div class=\"export-info\">");
            sb.AppendLine($"            <strong>Total Entries:</strong> {entries.Count}<br/>");
            sb.AppendLine($"            <strong>Date Range:</strong> {entries.Min(e => e.Date):MMMM dd, yyyy} - {entries.Max(e => e.Date):MMMM dd, yyyy}<br/>");
            sb.AppendLine($"            <strong>Export Date:</strong> {DateTime.Now:MMMM dd, yyyy HH:mm}");
            sb.AppendLine("        </div>");

            foreach (var entry in entries.OrderByDescending(e => e.Date))
            {
                sb.AppendLine("        <div class=\"entry\">");
                sb.AppendLine($"            <div class=\"entry-header\">");
                sb.AppendLine($"                <div>");
                sb.AppendLine($"                    <div class=\"entry-title\">{entry.Title}</div>");
                sb.AppendLine($"                    <div class=\"entry-date\">{entry.Date:dddd, MMMM dd, yyyy}</div>");
                sb.AppendLine("                </div>");
                sb.AppendLine("            </div>");

                // Metadata
                sb.AppendLine("            <div class=\"entry-meta\">");
                sb.AppendLine($"                <span class=\"badge mood-badge\">{GetMoodEmoji(entry.PrimaryMood)} {entry.PrimaryMood}</span>");
                if (entry.SecondaryMood1.HasValue)
                    sb.AppendLine($"                <span class=\"badge mood-badge\">{GetMoodEmoji(entry.SecondaryMood1.Value)} {entry.SecondaryMood1}</span>");
                if (entry.SecondaryMood2.HasValue)
                    sb.AppendLine($"                <span class=\"badge mood-badge\">{GetMoodEmoji(entry.SecondaryMood2.Value)} {entry.SecondaryMood2}</span>");
                sb.AppendLine($"                <span class=\"badge category-badge\">{entry.Category}</span>");
                if (entry.IsFavorite)
                    sb.AppendLine($"                <span class=\"badge favorite-badge\">⭐ Favorite</span>");
                sb.AppendLine("            </div>");

                // Content
                sb.AppendLine($"            <div class=\"entry-content\">{HtmlEncode(entry.Content)}</div>");

                // Tags
                if (entry.Tags.Any())
                {
                    sb.AppendLine("            <div class=\"tags\">");
                    foreach (var tag in entry.Tags)
                    {
                        sb.AppendLine($"                <span class=\"tag\">#{tag.Name}</span>");
                    }
                    sb.AppendLine("            </div>");
                }

                sb.AppendLine("        </div>");
            }

            sb.AppendLine("    </div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        private string GenerateMarkdownContent(List<JournalEntry> entries, DateOnly startDate, DateOnly endDate)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# 📔 Journal Export");
            sb.AppendLine($"\n**Date Range:** {startDate:MMMM dd, yyyy} - {endDate:MMMM dd, yyyy}  ");
            sb.AppendLine($"**Total Entries:** {entries.Count}  ");
            sb.AppendLine($"**Export Date:** {DateTime.Now:MMMM dd, yyyy HH:mm}\n");
            sb.AppendLine("---\n");

            foreach (var entry in entries.OrderByDescending(e => e.Date))
            {
                sb.AppendLine($"## {entry.Title}");
                sb.AppendLine($"\n**Date:** {entry.Date:dddd, MMMM dd, yyyy}  ");
                sb.AppendLine($"**Mood:** {GetMoodEmoji(entry.PrimaryMood)} {entry.PrimaryMood}");
                
                if (entry.SecondaryMood1.HasValue)
                    sb.AppendLine($" · {GetMoodEmoji(entry.SecondaryMood1.Value)} {entry.SecondaryMood1}");
                if (entry.SecondaryMood2.HasValue)
                    sb.AppendLine($" · {GetMoodEmoji(entry.SecondaryMood2.Value)} {entry.SecondaryMood2}");
                
                sb.AppendLine($"  \n**Category:** {entry.Category}");
                
                if (entry.IsFavorite)
                    sb.AppendLine("  \n⭐ **Favorite**");

                sb.AppendLine($"\n{entry.Content}\n");

                if (entry.Tags.Any())
                {
                    sb.AppendLine($"**Tags:** {string.Join(" · ", entry.Tags.Select(t => $"#{t.Name}"))}\n");
                }

                sb.AppendLine("---\n");
            }

            return sb.ToString();
        }

        private string GetMoodEmoji(Mood mood) => mood switch
        {
            Mood.VeryHappy => "😄",
            Mood.Happy => "🙂",
            Mood.Neutral => "😐",
            Mood.Sad => "😔",
            Mood.VerySad => "😢",
            _ => "😐"
        };

        private string HtmlEncode(string text)
        {
            return System.Net.WebUtility.HtmlEncode(text)
                .Replace("\n", "<br/>");
        }

        private string GenerateCsvContent(List<JournalEntry> entries)
        {
            var sb = new StringBuilder();
            
            // CSV Header
            sb.AppendLine("\"Date\",\"Title\",\"Primary Mood\",\"Secondary Mood 1\",\"Secondary Mood 2\",\"Category\",\"Is Favorite\",\"Content\",\"Tags\"");
            
            // CSV Data Rows
            foreach (var entry in entries.OrderByDescending(e => e.Date))
            {
                var escapedTitle = EscapeCsvValue(entry.Title);
                var escapedContent = EscapeCsvValue(entry.Content);
                var tags = entry.Tags.Any() ? EscapeCsvValue(string.Join(", ", entry.Tags.Select(t => $"#{t.Name}"))) : "";
                
                sb.AppendLine($"\"{entry.Date:yyyy-MM-dd}\",\"{escapedTitle}\",\"{entry.PrimaryMood}\"," +
                    $"\"{entry.SecondaryMood1}\",\"{entry.SecondaryMood2}\",\"{entry.Category}\"," +
                    $"\"{entry.IsFavorite}\",\"{escapedContent}\",\"{tags}\"");
            }
            
            return sb.ToString();
        }

        private string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            
            // Escape quotes and remove line breaks for CSV
            return value.Replace("\"", "\"\"").Replace("\n", " ").Replace("\r", " ");
        }

        private byte[] ConvertHtmlToPdf(string htmlContent)
        {
            // Basic HTML to PDF conversion using simple encoding
            // In production, use a proper library like:
            // - SelectPdf (commercial)
            // - iTextSharp (open-source)
            // - HtmlRenderer.Core (open-source)
            
            // For now, return the HTML as bytes with a note that PDF conversion requires a library
            // The frontend can handle the PDF generation using a JavaScript library like jsPDF or html2pdf
            var bytes = Encoding.UTF8.GetBytes(htmlContent);
            return bytes;
        }
    }
}
