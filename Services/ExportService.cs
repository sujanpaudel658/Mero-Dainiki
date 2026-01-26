using Mero_Dainiki.Entities;
using Mero_Dainiki.Common;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PdfColors = QuestPDF.Helpers.Colors;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Mero_Dainiki.Services
{
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
        private readonly MarkdownPipeline _pipeline;

        public ExportService(IJournalService journalService)
        {
            _journalService = journalService;
            QuestPDF.Settings.License = LicenseType.Community;
            _pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        }

        public Task<ServiceResult<string>> ExportToHtmlAsync(List<JournalEntry> entries, DateOnly startDate, DateOnly endDate)
        {
            try
            {
                var htmlContent = GenerateHtmlContent(entries, startDate, endDate);
                return Task.FromResult(new ServiceResult<string> { Success = true, Data = htmlContent });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ServiceResult<string> { Success = false, ErrorMessage = ex.Message });
            }
        }

        public Task<ServiceResult<byte[]>> ExportToHtmlBytesAsync(List<JournalEntry> entries, DateOnly startDate, DateOnly endDate)
        {
            try
            {
                var htmlContent = GenerateHtmlContent(entries, startDate, endDate);
                return Task.FromResult(new ServiceResult<byte[]> { Success = true, Data = Encoding.UTF8.GetBytes(htmlContent) });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ServiceResult<byte[]> { Success = false, ErrorMessage = ex.Message });
            }
        }

        public Task<ServiceResult<string>> ExportToMarkdownAsync(List<JournalEntry> entries, DateOnly startDate, DateOnly endDate)
        {
            try
            {
                var markdown = GenerateMarkdownContent(entries, startDate, endDate);
                return Task.FromResult(new ServiceResult<string> { Success = true, Data = markdown });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ServiceResult<string> { Success = false, ErrorMessage = ex.Message });
            }
        }

        public Task<ServiceResult<string>> ExportToCsvAsync(List<JournalEntry> entries, DateOnly startDate, DateOnly endDate)
        {
            try
            {
                var csv = GenerateCsvContent(entries);
                return Task.FromResult(new ServiceResult<string> { Success = true, Data = csv });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ServiceResult<string> { Success = false, ErrorMessage = ex.Message });
            }
        }

        public Task<ServiceResult<byte[]>> ExportToPdfAsync(List<JournalEntry> entries, DateOnly startDate, DateOnly endDate)
        {
            try
            {
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(1.5f, Unit.Centimetre);
                        page.PageColor(PdfColors.White);
                        page.DefaultTextStyle(x => x.FontFamily("Verdana").FontSize(10).FontColor(PdfColors.Grey.Darken3));

                        page.Header().PaddingBottom(1, Unit.Centimetre).Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Mero Dainiki").SemiBold().FontSize(28).FontFamily("Helvetica").FontColor(PdfColors.Indigo.Medium);
                                col.Item().Text("Personal Journal Archive").FontSize(10).FontColor(PdfColors.Grey.Medium);
                            });

                            row.RelativeItem().AlignRight().Column(col =>
                            {
                                col.Item().Text($"{startDate:MMM d, yyyy} — {endDate:MMM d, yyyy}").FontSize(10).SemiBold();
                                col.Item().Text($"{entries.Count} Total Entries").FontSize(9).FontColor(PdfColors.Grey.Medium);
                            });
                        });

                        page.Content().Column(mainCol =>
                        {
                            foreach (var entry in entries.OrderByDescending(e => e.Date))
                            {
                                mainCol.Item().PaddingBottom(0.8f, Unit.Centimetre).Decoration(decoration =>
                                {
                                    decoration.Before().PaddingBottom(5).Row(row =>
                                    {
                                        row.RelativeItem().Text(entry.Title).FontSize(16).SemiBold().FontColor(PdfColors.Grey.Darken4);
                                        row.AutoItem().AlignRight().Text(entry.Date.ToString("dddd, MMMM d, yyyy")).FontSize(9).FontColor(PdfColors.Grey.Medium);
                                    });

                                    decoration.Content().Column(entryCol =>
                                    {
                                        entryCol.Item().PaddingBottom(10).Row(row =>
                                        {
                                            row.Spacing(8);
                                            row.AutoItem().Border(0.5f).BorderColor(GetMoodColor(entry.PrimaryMood)).PaddingHorizontal(6).PaddingVertical(2).Text($"{GetMoodEmoji(entry.PrimaryMood)} {entry.PrimaryMood}").FontSize(8).FontColor(GetMoodColor(entry.PrimaryMood));
                                            row.AutoItem().Background(PdfColors.Grey.Lighten4).PaddingHorizontal(6).PaddingVertical(2).Text(entry.Category.ToString()).FontSize(8).FontColor(PdfColors.Grey.Darken2);
                                            if (entry.IsFavorite) row.AutoItem().Text("★").FontSize(10).FontColor(PdfColors.Amber.Medium);
                                        });

                                        // Render Content with Markdown support
                                        var contentContainer = entryCol.Item();
                                        RenderMarkdownToContainer(contentContainer, entry.Content);

                                        if (entry.Tags.Any())
                                        {
                                            entryCol.Item().PaddingTop(10).Text(t =>
                                            {
                                                t.Span("Tags: ").FontSize(8).FontColor(PdfColors.Grey.Medium);
                                                t.Span(string.Join(", ", entry.Tags.Select(x => "#" + x.Name))).FontSize(8).Italic().FontColor(PdfColors.Indigo.Lighten1);
                                            });
                                        }
                                    });

                                    decoration.After().PaddingTop(15).LineHorizontal(0.5f).LineColor(PdfColors.Grey.Lighten3);
                                });
                            }
                        });

                        page.Footer().PaddingTop(1, Unit.Centimetre).AlignCenter().Text(x =>
                        {
                            x.Span("Generated via Mero Dainiki · Page ").FontSize(8).FontColor(PdfColors.Grey.Medium);
                            x.CurrentPageNumber().FontSize(8).FontColor(PdfColors.Grey.Medium);
                        });
                    });
                });

                return Task.FromResult(new ServiceResult<byte[]> { Success = true, Data = document.GeneratePdf() });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ServiceResult<byte[]> { Success = false, ErrorMessage = ex.Message });
            }
        }


        private void RenderMarkdownToContainer(QuestPDF.Infrastructure.IContainer container, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return;

            var doc = Markdown.Parse(content, _pipeline);
            container.Column(col => 
            {
                foreach (var block in doc)
                {
                    if (block is ParagraphBlock paragraph)
                    {
                        col.Item().Text(text => {
                            foreach (var inline in paragraph.Inline)
                            {
                                ProcessInline(text, inline);
                            }
                        });
                    }
                    else if (block is HeadingBlock heading)
                    {
                        col.Item().PaddingTop(10).Text(text => {
                            foreach(var inline in heading.Inline) ProcessInline(text, inline, true);
                        });
                    }
                    else if (block is ListBlock list)
                    {
                        foreach (var item in list)
                        {
                            if (item is ListItemBlock listItem)
                            {
                                col.Item().Row(row =>
                                {
                                    row.ConstantItem(20).AlignRight().PaddingRight(5).Text("•");
                                    row.RelativeItem().Text(t => {
                                        foreach(var subBlock in listItem)
                                            if(subBlock is ParagraphBlock p) 
                                                foreach(var i in p.Inline) ProcessInline(t, i);
                                    });
                                });
                            }
                        }
                    }
                }
            });
        }

        private void ProcessInline(TextDescriptor text, Inline inline, bool isHeading = false)
        {
            if (inline == null) return;

            switch (inline)
            {
                case LiteralInline literal:
                    var spanLiteral = text.Span(literal.ToString()).FontSize(isHeading ? 14 : 10);
                    if (isHeading) spanLiteral.SemiBold();
                    break;
                case EmphasisInline emphasis:
                    var textToWait = "";
                    foreach (var child in emphasis)
                    {
                        if (child is LiteralInline childLiteral) textToWait += childLiteral.ToString();
                    }
                    var span = text.Span(textToWait).FontSize(isHeading ? 14 : 10);
                    if (emphasis.DelimiterCount == 2) span.SemiBold();
                    else span.Italic();
                    break;
                case LineBreakInline _:
                    // TextDescriptor handles breaks within spans usually, but we can't easily force it here
                    // Just add a space or let it wrap
                    text.Span(" ");
                    break;
                case ContainerInline container:
                    foreach(var child in container) ProcessInline(text, child, isHeading);
                    break;
            }
        }



        private string GetMoodColor(Mood mood) => mood switch
        {
            Mood.VeryHappy => PdfColors.Green.Medium,
            Mood.Happy => PdfColors.LightGreen.Medium,
            Mood.Neutral => PdfColors.Blue.Medium,
            Mood.Sad => PdfColors.Orange.Medium,
            Mood.VerySad => PdfColors.Red.Medium,
            _ => PdfColors.Grey.Medium
        };

        public string GenerateFilename(string format, DateOnly startDate, DateOnly endDate)
        {
            var ext = format.ToLower() == "pdf" ? ".pdf" : ".html";
            return $"MeroDainiki_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}{ext}";
        }

        private string GenerateHtmlContent(List<JournalEntry> entries, DateOnly start, DateOnly end)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'>");
            sb.AppendLine("<style>body{font-family:system-ui;padding:40px;color:#1e293b;line-height:1.6;max-width:800px;margin:auto}");
            sb.AppendLine("h1{color:#6366f1;border-bottom:2px solid #eef2ff;padding-bottom:10px} .entry{margin-bottom:40px;border-bottom:1px solid #f1f5f9;padding-bottom:30px}");
            sb.AppendLine(".meta{font-size:12px;color:#64748b;margin-bottom:15px} .badge{padding:2px 8px;border-radius:4px;background:#f1f5f9;margin-right:10px}</style>");
            sb.AppendLine("</head><body><header><h1>Journal Archives</h1><p>" + start + " to " + end + "</p></header>");

            foreach (var entry in entries.OrderByDescending(e => e.Date))
            {
                sb.AppendLine("<div class='entry'>");
                sb.AppendLine("<h2>" + entry.Title + "</h2>");
                sb.AppendLine("<div class='meta'><span class='badge'>" + entry.Date.ToShortDateString() + "</span> " + entry.PrimaryMood + " · " + entry.Category + "</div>");
                sb.AppendLine("<div>" + Markdown.ToHtml(entry.Content, _pipeline) + "</div>");
                sb.AppendLine("</div>");
            }
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private string GenerateMarkdownContent(List<JournalEntry> entries, DateOnly start, DateOnly end)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Journal Export: " + start + " to " + end + "\n");
            foreach (var entry in entries)
            {
                sb.AppendLine("## " + entry.Title);
                sb.AppendLine("**Date:** " + entry.Date + " | **Mood:** " + entry.PrimaryMood + "\n");
                sb.AppendLine(entry.Content + "\n\n---\n");
            }
            return sb.ToString();
        }

        private string GenerateCsvContent(List<JournalEntry> entries)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Date,Title,Mood,Category,Content");
            foreach (var entry in entries)
            {
                sb.AppendLine($"{entry.Date},\"{entry.Title.Replace("\"", "\"\"")}\",{entry.PrimaryMood},{entry.Category},\"{entry.Content.Replace("\"", "\"\"")}\"");
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
    }
}
