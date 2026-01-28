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
    /// <summary>
    /// Service for exporting journal data into various formats (PDF, HTML, CSV, Markdown).
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
        private readonly MarkdownPipeline _pipeline;

        public ExportService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
            _pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        }

        private async Task<ServiceResult<T>> TryAction<T>(Func<T> action) => 
            await Task.Run(() => { try { return ServiceResult<T>.Ok(action()); } catch (Exception ex) { return ServiceResult<T>.Fail(ex.Message); } });

        public Task<ServiceResult<string>> ExportToHtmlAsync(List<JournalEntry> entries, DateOnly start, DateOnly end) => TryAction(() => GenerateHtmlContent(entries, start, end));
        public Task<ServiceResult<byte[]>> ExportToHtmlBytesAsync(List<JournalEntry> entries, DateOnly start, DateOnly end) => TryAction(() => Encoding.UTF8.GetBytes(GenerateHtmlContent(entries, start, end)));
        public Task<ServiceResult<string>> ExportToMarkdownAsync(List<JournalEntry> entries, DateOnly start, DateOnly end) => TryAction(() => GenerateMarkdownContent(entries, start, end));
        public Task<ServiceResult<string>> ExportToCsvAsync(List<JournalEntry> entries, DateOnly _, DateOnly __) => TryAction(() => GenerateCsvContent(entries));

        /// <summary>
        /// Generates a professional PDF document using the QuestPDF library.
        /// Handles complex layouts, mood emojis, and markdown-to-pdf rendering.
        /// </summary>
        public Task<ServiceResult<byte[]>> ExportToPdfAsync(List<JournalEntry> entries, DateOnly start, DateOnly end) => TryAction(() => {
            var doc = Document.Create(container => {
                container.Page(page => {
                    // Global Page Configuration
                    page.Size(PageSizes.A4); 
                    page.Margin(1.5f, Unit.Centimetre); 
                    page.PageColor(PdfColors.White);
                    page.DefaultTextStyle(x => x.FontFamily("Verdana").FontSize(10).FontColor(PdfColors.Grey.Darken3));

                    // Professional Header with Branding
                    page.Header().PaddingBottom(1, Unit.Centimetre).Row(row => {
                        row.RelativeItem().Column(col => {
                            col.Item().Text("Mero Dainiki").SemiBold().FontSize(28).FontFamily("Helvetica").FontColor(PdfColors.Indigo.Medium);
                            col.Item().Text("Personal Journal Archive").FontSize(10).FontColor(PdfColors.Grey.Medium);
                        });
                        row.RelativeItem().AlignRight().Column(col => {
                            col.Item().Text($"{start:MMM d, yyyy} — {end:MMM d, yyyy}").FontSize(10).SemiBold();
                            col.Item().Text($"{entries.Count} Total Entries").FontSize(9).FontColor(PdfColors.Grey.Medium);
                        });
                    });

                    // Main Content Loop for Entries
                    page.Content().Column(mainCol => {
                        foreach (var entry in entries.OrderByDescending(e => e.Date)) {
                            mainCol.Item().PaddingBottom(0.8f, Unit.Centimetre).Decoration(dec => {
                                // Entry Header: Title and Date
                                dec.Before().PaddingBottom(5).Row(row => {
                                    row.RelativeItem().Text(entry.Title).FontSize(16).SemiBold().FontColor(PdfColors.Grey.Darken4);
                                    row.AutoItem().AlignRight().Text(entry.Date.ToString("dddd, MMMM d, yyyy")).FontSize(9).FontColor(PdfColors.Grey.Medium);
                                });

                                // Entry Metadata: Mood and Category
                                dec.Content().Column(entryCol => {
                                    entryCol.Item().PaddingBottom(10).Row(row => {
                                        row.Spacing(8);
                                        // Visual Mood Badge
                                        row.AutoItem().Border(0.5f).BorderColor(GetMoodColor(entry.PrimaryMood)).PaddingHorizontal(6).PaddingVertical(2)
                                           .Text($"{GetMoodEmoji(entry.PrimaryMood)} {entry.PrimaryMood}").FontSize(8).FontColor(GetMoodColor(entry.PrimaryMood));
                                        
                                        row.AutoItem().Background(PdfColors.Grey.Lighten4).PaddingHorizontal(6).PaddingVertical(2)
                                           .Text(entry.Category.ToString()).FontSize(8).FontColor(PdfColors.Grey.Darken2);
                                        
                                        if (entry.IsFavorite) row.AutoItem().Text("★").FontSize(10).FontColor(PdfColors.Amber.Medium);
                                    });

                                    // Render Markdown content to PDF blocks
                                    RenderMarkdownToContainer(entryCol.Item(), entry.Content);

                                    // Tags section
                                    if (entry.Tags.Any()) entryCol.Item().PaddingTop(10).Text(t => {
                                        t.Span("Tags: ").FontSize(8).FontColor(PdfColors.Grey.Medium);
                                        t.Span(string.Join(", ", entry.Tags.Select(x => "#" + x.Name))).FontSize(8).Italic().FontColor(PdfColors.Indigo.Lighten1);
                                    });
                                });
                                dec.After().PaddingTop(15).LineHorizontal(0.5f).LineColor(PdfColors.Grey.Lighten3);
                            });
                        }
                    });

                    // Dynamic Sticky Footer
                    page.Footer().PaddingTop(1, Unit.Centimetre).AlignCenter().Text(x => {
                        x.Span("Generated via Mero Dainiki · Page ").FontSize(8).FontColor(PdfColors.Grey.Medium);
                        x.CurrentPageNumber().FontSize(8).FontColor(PdfColors.Grey.Medium);
                    });
                });
            });
            return doc.GeneratePdf();
        });

        /// <summary>
        /// Highly specialized method to parse Markdown syntax and convert it into QuestPDF layout blocks.
        /// Supports Paragraphs, Headings, and Bulleted Lists.
        /// </summary>
        private void RenderMarkdownToContainer(QuestPDF.Infrastructure.IContainer container, string content) {
            if (string.IsNullOrWhiteSpace(content)) return;
            var doc = Markdown.Parse(content, _pipeline);
            container.Column(col => {
                foreach (var block in doc) {
                    // Map Markdown blocks to PDF layout elements
                    if (block is ParagraphBlock p) col.Item().Text(t => { foreach (var i in p.Inline) ProcessInline(t, i); });
                    else if (block is HeadingBlock h) col.Item().PaddingTop(10).Text(t => { foreach(var i in h.Inline) ProcessInline(t, i, true); });
                    else if (block is ListBlock l) foreach (var item in l) if (item is ListItemBlock li) col.Item().Row(row => {
                        row.ConstantItem(20).AlignRight().PaddingRight(5).Text("•"); // Simple bullet point logic
                        row.RelativeItem().Text(t => { foreach(var sb in li) if(sb is ParagraphBlock sp) foreach(var si in sp.Inline) ProcessInline(t, si); });
                    });
                }
            });
        }

        private void ProcessInline(TextDescriptor text, Inline inline, bool isHeading = false) {
            if (inline == null) return;
            switch (inline) {
                case LiteralInline l: var sl = text.Span(l.ToString()).FontSize(isHeading ? 14 : 10); if (isHeading) sl.SemiBold(); break;
                case EmphasisInline e: var tw = ""; foreach (var c in e) if (c is LiteralInline cl) tw += cl.ToString();
                    var s = text.Span(tw).FontSize(isHeading ? 14 : 10); if (e.DelimiterCount == 2) s.SemiBold(); else s.Italic(); break;
                case LineBreakInline _: text.Span(" "); break;
                case ContainerInline c: foreach(var ch in c) ProcessInline(text, ch, isHeading); break;
            }
        }

        private string GetMoodColor(Mood mood) => mood switch {
            Mood.VeryHappy => PdfColors.Green.Medium, Mood.Happy => PdfColors.LightGreen.Medium,
            Mood.Neutral => PdfColors.Blue.Medium, Mood.Sad => PdfColors.Orange.Medium,
            Mood.VerySad => PdfColors.Red.Medium, _ => PdfColors.Grey.Medium
        };

        private string GetMoodEmoji(Mood mood) => mood switch {
            Mood.VeryHappy => "😄", Mood.Happy => "🙂", Mood.Neutral => "😐",
            Mood.Sad => "😔", Mood.VerySad => "😢", _ => "😐"
        };

        public string GenerateFilename(string format, DateOnly start, DateOnly end) => $"MeroDainiki_{start:yyyyMMdd}_{end:yyyyMMdd}.{(format.ToLower() == "pdf" ? "pdf" : "html")}";

        private string GenerateHtmlContent(List<JournalEntry> entries, DateOnly start, DateOnly end) {
            var sb = new StringBuilder("<!DOCTYPE html><html><head><meta charset='utf-8'><style>body{font-family:system-ui;padding:40px;color:#1e293b;line-height:1.6;max-width:800px;margin:auto}h1{color:#6366f1;border-bottom:2px solid #eef2ff;padding-bottom:10px}.entry{margin-bottom:40px;border-bottom:1px solid #f1f5f9;padding-bottom:30px}.meta{font-size:12px;color:#64748b;margin-bottom:15px}.badge{padding:2px 8px;border-radius:4px;background:#f1f5f9;margin-right:10px}</style></head><body>");
            sb.Append($"<header><h1>Journal Archives</h1><p>{start} to {end}</p></header>");
            foreach (var entry in entries.OrderByDescending(e => e.Date)) {
                sb.Append($"<div class='entry'><h2>{entry.Title}</h2><div class='meta'><span class='badge'>{entry.Date:MMM dd, yyyy}</span> {entry.PrimaryMood} · {entry.Category}</div><div>{Markdown.ToHtml(entry.Content, _pipeline)}</div></div>");
            }
            return sb.Append("</body></html>").ToString();
        }

        private string GenerateMarkdownContent(List<JournalEntry> entries, DateOnly start, DateOnly end) {
            var sb = new StringBuilder($"# Journal Export: {start} to {end}\n\n");
            foreach (var entry in entries) sb.AppendLine($"## {entry.Title}\n**Date:** {entry.Date} | **Mood:** {entry.PrimaryMood}\n\n{entry.Content}\n\n---\n");
            return sb.ToString();
        }

        private string GenerateCsvContent(List<JournalEntry> entries) {
            var sb = new StringBuilder("Date,Title,Mood,Category,Content\n");
            foreach (var entry in entries) sb.AppendLine($"{entry.Date},\"{entry.Title.Replace("\"", "\"\"")}\",{entry.PrimaryMood},{entry.Category},\"{entry.Content.Replace("\"", "\"\"")}\"");
            return sb.ToString();
        }
    }
}
