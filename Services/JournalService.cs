using Microsoft.EntityFrameworkCore;
using Mero_Dainiki.Common;
using Mero_Dainiki.Data;
using Mero_Dainiki.Entities;
using Mero_Dainiki.Models;

namespace Mero_Dainiki.Services
{
    /// <summary>
    /// Interface for journal entry operations
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

    /// <summary>
    /// Journal service implementation with EF Core SQLite
    /// </summary>
    public class JournalService : BaseService, IJournalService
    {
        public JournalService(AppDbContext context) : base(context) { }

        public async Task<ServiceResult<JournalEntry>> GetEntryByDateAsync(DateTime date)
        {
            try
            {
                var entry = await _context.JournalEntries
                    .Include(e => e.Tags)
                    .FirstOrDefaultAsync(e => e.UserId == CurrentUserId && e.Date.Date == date.Date);

                return entry != null
                    ? ServiceResult<JournalEntry>.Ok(entry)
                    : ServiceResult<JournalEntry>.Fail("No entry found for this date.");
            }
            catch (Exception ex)
            {
                return ServiceResult<JournalEntry>.Fail($"Error: {ex.Message}");
            }
        }

        public async Task<ServiceResult<JournalEntry>> GetEntryByIdAsync(int id)
        {
            try
            {
                var entry = await _context.JournalEntries
                    .Include(e => e.Tags)
                    .FirstOrDefaultAsync(e => e.UserId == CurrentUserId && e.Id == id);

                return entry != null
                    ? ServiceResult<JournalEntry>.Ok(entry)
                    : ServiceResult<JournalEntry>.Fail("Entry not found.");
            }
            catch (Exception ex)
            {
                return ServiceResult<JournalEntry>.Fail($"Error: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<JournalEntry>>> GetEntriesAsync(int page = 1, int pageSize = 10)
        {
            try
            {
                var entries = await _context.JournalEntries
                    .Include(e => e.Tags)
                    .Where(e => e.UserId == CurrentUserId)
                    .OrderByDescending(e => e.Date)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return ServiceResult<List<JournalEntry>>.Ok(entries);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<JournalEntry>>.Fail($"Error: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<JournalEntry>>> SearchEntriesAsync(
            string? searchText, DateTime? startDate, DateTime? endDate, Mood? mood, List<int>? tagIds)
        {
            try
            {
                var query = _context.JournalEntries.Include(e => e.Tags).Where(e => e.UserId == CurrentUserId).AsQueryable();

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    query = query.Where(e => e.Title.Contains(searchText) || e.Content.Contains(searchText));
                }

                if (startDate.HasValue) query = query.Where(e => e.Date >= startDate.Value);
                if (endDate.HasValue) query = query.Where(e => e.Date <= endDate.Value);
                if (mood.HasValue) query = query.Where(e => e.PrimaryMood == mood.Value);

                if (tagIds != null && tagIds.Any())
                {
                    query = query.Where(e => e.Tags.Any(t => tagIds.Contains(t.Id)));
                }

                var entries = await query.OrderByDescending(e => e.Date).ToListAsync();
                return ServiceResult<List<JournalEntry>>.Ok(entries);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<JournalEntry>>.Fail($"Error: {ex.Message}");
            }
        }

        public async Task<ServiceResult<JournalEntry>> CreateEntryAsync(JournalEntryViewModel model)
        {
            try
            {
                if (await _context.JournalEntries.AnyAsync(e => e.UserId == CurrentUserId && e.Date.Date == model.Date.Date))
                {
                    return ServiceResult<JournalEntry>.Fail("An entry already exists for this date.");
                }

                var entry = new JournalEntry
                {
                    UserId = CurrentUserId,
                    Title = model.Title,
                    Content = model.Content,
                    Date = model.Date,
                    PrimaryMood = model.PrimaryMood,
                    SecondaryMood1 = model.SecondaryMoods.ElementAtOrDefault(0),
                    SecondaryMood2 = model.SecondaryMoods.ElementAtOrDefault(1),
                    Category = model.Category,
                    IsFavorite = model.IsFavorite,
                    CreatedAt = DateTime.UtcNow
                };

                if (model.TagIds.Any())
                {
                    entry.Tags = await _context.Tags.Where(t => model.TagIds.Contains(t.Id)).ToListAsync();
                }

                _context.JournalEntries.Add(entry);
                await _context.SaveChangesAsync();

                return ServiceResult<JournalEntry>.Ok(entry);
            }
            catch (Exception ex)
            {
                return ServiceResult<JournalEntry>.Fail($"Error: {ex.Message}");
            }
        }

        public async Task<ServiceResult<JournalEntry>> UpdateEntryAsync(JournalEntryViewModel model)
        {
            try
            {
                var entry = await _context.JournalEntries
                    .Include(e => e.Tags)
                    .FirstOrDefaultAsync(e => e.UserId == CurrentUserId && e.Id == model.Id);

                if (entry == null) return ServiceResult<JournalEntry>.Fail("Entry not found.");

                if (await _context.JournalEntries.AnyAsync(e => e.UserId == CurrentUserId && e.Date.Date == model.Date.Date && e.Id != model.Id))
                {
                    return ServiceResult<JournalEntry>.Fail("Another entry already exists for this date.");
                }

                entry.Title = model.Title;
                entry.Content = model.Content;
                entry.PrimaryMood = model.PrimaryMood;
                entry.SecondaryMood1 = model.SecondaryMoods.ElementAtOrDefault(0);
                entry.SecondaryMood2 = model.SecondaryMoods.ElementAtOrDefault(1);
                entry.Category = model.Category;
                entry.IsFavorite = model.IsFavorite;
                entry.Date = model.Date;
                entry.UpdatedAt = DateTime.UtcNow;

                entry.Tags.Clear();
                if (model.TagIds.Any())
                {
                    var tags = await _context.Tags.Where(t => model.TagIds.Contains(t.Id)).ToListAsync();
                    foreach (var tag in tags) entry.Tags.Add(tag);
                }

                await _context.SaveChangesAsync();
                return ServiceResult<JournalEntry>.Ok(entry);
            }
            catch (Exception ex)
            {
                return ServiceResult<JournalEntry>.Fail($"Error: {ex.Message}");
            }
        }

        public async Task<ServiceResult> DeleteEntryAsync(int id)
        {
            try
            {
                var entry = await _context.JournalEntries.FirstOrDefaultAsync(e => e.UserId == CurrentUserId && e.Id == id);
                if (entry == null) return ServiceResult.Fail("Entry not found.");

                _context.JournalEntries.Remove(entry);
                await _context.SaveChangesAsync();
                return ServiceResult.Ok();
            }
            catch (Exception ex)
            {
                return ServiceResult.Fail($"Error: {ex.Message}");
            }
        }

        public async Task<ServiceResult<MoodAnalyticsModel>> GetAnalyticsAsync()
        {
            try
            {
                var entries = await _context.JournalEntries.Include(e => e.Tags).Where(e => e.UserId == CurrentUserId).ToListAsync();
                if (!entries.Any()) return ServiceResult<MoodAnalyticsModel>.Ok(new MoodAnalyticsModel());

                var analytics = new MoodAnalyticsModel
                {
                    TotalEntries = entries.Count,
                    MoodDistribution = entries.GroupBy(e => e.PrimaryMood.ToString()).ToDictionary(g => g.Key, g => g.Count()),
                    TagUsage = entries.SelectMany(e => e.Tags).GroupBy(t => t.Name).ToDictionary(g => g.Key, g => g.Count()),
                    AverageWordCount = entries.Average(e => e.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length),
                    WordCountTrends = entries.OrderBy(e => e.Date).Select(e => new WordCountTrend { 
                        Date = e.Date, 
                        WordCount = e.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length 
                    }).ToList()
                };

                analytics.MostFrequentMood = analytics.MoodDistribution.OrderByDescending(x => x.Value).FirstOrDefault().Key;
                analytics.MostUsedTag = analytics.TagUsage.OrderByDescending(x => x.Value).FirstOrDefault().Key;

                var streakResult = await GetStreakAsync();
                if (streakResult.Success)
                {
                    analytics.CurrentStreak = streakResult.Data.current;
                    analytics.LongestStreak = streakResult.Data.longest;
                }

                return ServiceResult<MoodAnalyticsModel>.Ok(analytics);
            }
            catch (Exception ex)
            {
                return ServiceResult<MoodAnalyticsModel>.Fail($"Error: {ex.Message}");
            }
        }

        public async Task<ServiceResult<(int current, int longest)>> GetStreakAsync()
        {
            try
            {
                var dates = await _context.JournalEntries
                    .Where(e => e.UserId == CurrentUserId)
                    .Select(e => e.Date.Date)
                    .Distinct()
                    .OrderByDescending(d => d)
                    .ToListAsync();

                if (!dates.Any()) return ServiceResult<(int, int)>.Ok((0, 0));

                int currentStreak = 0;
                int longestStreak = 0;
                int tempStreak = 1;

                var today = DateTime.Today;
                if (dates.Contains(today) || dates.Contains(today.AddDays(-1)))
                {
                    var startDate = dates.Contains(today) ? today : today.AddDays(-1);
                    currentStreak = 1;
                    var checkDate = startDate.AddDays(-1);
                    while (dates.Contains(checkDate))
                    {
                        currentStreak++;
                        checkDate = checkDate.AddDays(-1);
                    }
                }

                for (int i = 1; i < dates.Count; i++)
                {
                    if ((dates[i - 1] - dates[i]).Days == 1) tempStreak++;
                    else
                    {
                        longestStreak = Math.Max(longestStreak, tempStreak);
                        tempStreak = 1;
                    }
                }
                longestStreak = Math.Max(longestStreak, tempStreak);

                return ServiceResult<(int, int)>.Ok((currentStreak, longestStreak));
            }
            catch (Exception ex)
            {
                return ServiceResult<(int, int)>.Fail($"Error: {ex.Message}");
            }
        }
    }
}

