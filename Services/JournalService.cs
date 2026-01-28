using Microsoft.EntityFrameworkCore;
using Mero_Dainiki.Common;
using Mero_Dainiki.Data;
using Mero_Dainiki.Entities;
using Mero_Dainiki.Models;

namespace Mero_Dainiki.Services
{
    /// <summary>
    /// Service interface for handling all journal entry related operations.
    /// Provides methods for CRUD, search, analytics, and streak tracking.
    /// </summary>
    public interface IJournalService
    {
        Task<ServiceResult<JournalEntry>> GetEntryByDateAsync(DateTime date);
        Task<ServiceResult<JournalEntry>> GetEntryByIdAsync(int id);
        Task<ServiceResult<List<JournalEntry>>> GetEntriesAsync(int page = 1, int pageSize = 10);
        Task<ServiceResult<List<JournalEntry>>> SearchEntriesAsync(string? searchText, DateTime? startDate, DateTime? endDate, Mood? mood, List<int>? tagIds);
        Task<ServiceResult<JournalEntry>> CreateEntryAsync(JournalEntryViewModel model);
        Task<ServiceResult<JournalEntry>> UpdateEntryAsync(JournalEntryViewModel model);
        Task<ServiceResult> DeleteEntryAsync(int id);
        Task<ServiceResult<MoodAnalyticsModel>> GetAnalyticsAsync();
        Task<ServiceResult<(int current, int longest)>> GetStreakAsync();
    }

    public class JournalService : BaseService, IJournalService
    {
        public JournalService(AppDbContext context) : base(context) { }

        /// <summary>
        /// Retrieves a journal entry for a specific date. 
        /// Enforces the business rule that only one entry can exist per day per user.
        /// </summary>
        public Task<ServiceResult<JournalEntry>> GetEntryByDateAsync(DateTime date) =>
            ExecuteAsync(async () => {
                var entry = await _context.JournalEntries.Include(e => e.Tags)
                    .FirstOrDefaultAsync(e => e.UserId == CurrentUserId && e.Date.Date == date.Date);
                return entry ?? throw new Exception("No entry found for this date.");
            });

        public Task<ServiceResult<JournalEntry>> GetEntryByIdAsync(int id) =>
            ExecuteAsync(async () => {
                var entry = await _context.JournalEntries.Include(e => e.Tags)
                    .FirstOrDefaultAsync(e => e.UserId == CurrentUserId && e.Id == id);
                return entry ?? throw new Exception("Entry not found.");
            });

        /// <summary>
        /// Retrieves paginated list of journal entries.
        /// Part of the "Paginated Journal View" feature for optimized performance.
        /// </summary>
        public Task<ServiceResult<List<JournalEntry>>> GetEntriesAsync(int page = 1, int pageSize = 10) =>
            ExecuteAsync(() => _context.JournalEntries.Include(e => e.Tags)
                .Where(e => e.UserId == CurrentUserId)
                .OrderByDescending(e => e.Date)
                .Skip((page - 1) * pageSize) // Implementation of pagination: Skip records from previous pages
                .Take(pageSize)               // Implementation of pagination: Take only current page limit
                .ToListAsync());

        /// <summary>
        /// Advanced search and filter logic implementation.
        /// Supports filtering by text, date ranges, specific moods, and tags.
        /// </summary>
        public Task<ServiceResult<List<JournalEntry>>> SearchEntriesAsync(string? searchText, DateTime? startDate, DateTime? endDate, Mood? mood, List<int>? tagIds) =>
            ExecuteAsync(async () => {
                var query = _context.JournalEntries.Include(e => e.Tags).Where(e => e.UserId == CurrentUserId).AsQueryable();
                
                // Dynamic query building based on provided parameters
                if (!string.IsNullOrWhiteSpace(searchText)) query = query.Where(e => e.Title.Contains(searchText) || e.Content.Contains(searchText));
                if (startDate.HasValue) query = query.Where(e => e.Date >= startDate.Value);
                if (endDate.HasValue) query = query.Where(e => e.Date <= endDate.Value);
                if (mood.HasValue) query = query.Where(e => e.PrimaryMood == mood.Value);
                if (tagIds?.Any() == true) query = query.Where(e => e.Tags.Any(t => tagIds.Contains(t.Id)));
                
                return await query.OrderByDescending(e => e.Date).ToListAsync();
            });

        public Task<ServiceResult<JournalEntry>> CreateEntryAsync(JournalEntryViewModel model) =>
            ExecuteAsync(async () => {
                if (await _context.JournalEntries.AnyAsync(e => e.UserId == CurrentUserId && e.Date.Date == model.Date.Date))
                    throw new Exception("An entry already exists for this date.");

                var entry = new JournalEntry {
                    UserId = CurrentUserId, Title = model.Title, Content = model.Content, Date = model.Date,
                    PrimaryMood = model.PrimaryMood, SecondaryMood1 = model.SecondaryMoods.ElementAtOrDefault(0),
                    SecondaryMood2 = model.SecondaryMoods.ElementAtOrDefault(1), Category = model.Category,
                    IsFavorite = model.IsFavorite, CreatedAt = DateTime.UtcNow
                };

                if (model.TagIds.Any()) entry.Tags = await _context.Tags.Where(t => model.TagIds.Contains(t.Id)).ToListAsync();
                _context.JournalEntries.Add(entry);
                await _context.SaveChangesAsync();
                return entry;
            });

        public Task<ServiceResult<JournalEntry>> UpdateEntryAsync(JournalEntryViewModel model) =>
            ExecuteAsync(async () => {
                var entry = await _context.JournalEntries.Include(e => e.Tags).FirstOrDefaultAsync(e => e.UserId == CurrentUserId && e.Id == model.Id);
                if (entry == null) throw new Exception("Entry not found.");
                if (await _context.JournalEntries.AnyAsync(e => e.UserId == CurrentUserId && e.Date.Date == model.Date.Date && e.Id != model.Id))
                    throw new Exception("Another entry already exists for this date.");

                entry.Title = model.Title; entry.Content = model.Content; entry.PrimaryMood = model.PrimaryMood;
                entry.SecondaryMood1 = model.SecondaryMoods.ElementAtOrDefault(0); entry.SecondaryMood2 = model.SecondaryMoods.ElementAtOrDefault(1);
                entry.Category = model.Category; entry.IsFavorite = model.IsFavorite; entry.Date = model.Date; entry.UpdatedAt = DateTime.UtcNow;

                entry.Tags.Clear();
                if (model.TagIds.Any()) {
                    var tags = await _context.Tags.Where(t => model.TagIds.Contains(t.Id)).ToListAsync();
                    foreach (var tag in tags) entry.Tags.Add(tag);
                }
                await _context.SaveChangesAsync();
                return entry;
            });

        public Task<ServiceResult> DeleteEntryAsync(int id) =>
            ExecuteVoidAsync(async () => {
                var entry = await _context.JournalEntries.FirstOrDefaultAsync(e => e.UserId == CurrentUserId && e.Id == id);
                if (entry == null) throw new Exception("Entry not found.");
                _context.JournalEntries.Remove(entry);
                await _context.SaveChangesAsync();
            });

        public Task<ServiceResult<MoodAnalyticsModel>> GetAnalyticsAsync() =>
            ExecuteAsync(async () => {
                var entries = await _context.JournalEntries.Include(e => e.Tags).Where(e => e.UserId == CurrentUserId).ToListAsync();
                if (!entries.Any()) return new MoodAnalyticsModel();

                var analytics = new MoodAnalyticsModel {
                    TotalEntries = entries.Count,
                    MoodDistribution = entries.GroupBy(e => e.PrimaryMood.ToString()).ToDictionary(g => g.Key, g => g.Count()),
                    TagUsage = entries.SelectMany(e => e.Tags).GroupBy(t => t.Name).ToDictionary(g => g.Key, g => g.Count()),
                    AverageWordCount = entries.Average(e => GetWordCount(e.Content)),
                    WordCountTrends = entries.OrderBy(e => e.Date).Select(e => new WordCountTrend { Date = e.Date, WordCount = GetWordCount(e.Content) }).ToList()
                };

                analytics.MostFrequentMood = analytics.MoodDistribution.OrderByDescending(x => x.Value).FirstOrDefault().Key;
                analytics.MostUsedTag = analytics.TagUsage.OrderByDescending(x => x.Value).FirstOrDefault().Key;

                var streak = await GetStreakAsync();
                if (streak.Success) {
                    analytics.CurrentStreak = streak.Data.current;
                    analytics.LongestStreak = streak.Data.longest;
                }
                return analytics;
            });

        /// <summary>
        /// Calculates the user's current and all-time longest journaling streaks.
        /// Encourages consistency through numerical feedback.
        /// </summary>
        public Task<ServiceResult<(int current, int longest)>> GetStreakAsync() =>
            ExecuteAsync(async () => {
                var dates = await _context.JournalEntries
                    .Where(e => e.UserId == CurrentUserId)
                    .Select(e => e.Date.Date)
                    .Distinct()
                    .OrderByDescending(d => d)
                    .ToListAsync();

                if (!dates.Any()) return (0, 0);

                int current = 0, longest = 0, temp = 1;
                var today = DateTime.Today;

                // Logical block for current streak (must include today or yesterday)
                if (dates.Contains(today) || dates.Contains(today.AddDays(-1))) {
                    var check = dates.Contains(today) ? today : today.AddDays(-1);
                    while (dates.Contains(check)) { current++; check = check.AddDays(-1); }
                }

                // Mathematical iteration to find historical longest streak
                for (int i = 1; i < dates.Count; i++) {
                    if ((dates[i - 1] - dates[i]).Days == 1) temp++;
                    else { longest = Math.Max(longest, temp); temp = 1; }
                }
                return (current, Math.Max(longest, temp));
            });

        private static int GetWordCount(string text) => string.IsNullOrWhiteSpace(text) ? 0 : text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
