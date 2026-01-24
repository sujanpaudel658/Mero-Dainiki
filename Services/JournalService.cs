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
    public class JournalService : IJournalService
    {
        private readonly AppDbContext _context;

        public JournalService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult<JournalEntry>> GetEntryByDateAsync(DateTime date)
        {
            try
            {
                var entry = await _context.JournalEntries
                    .Include(e => e.Tags)
                    .FirstOrDefaultAsync(e => e.Date.Date == date.Date);

                return entry != null
                    ? ServiceResult<JournalEntry>.Ok(entry)
                    : ServiceResult<JournalEntry>.Fail("No entry found for this date.");
            }
            catch (Exception ex)
            {
                return ServiceResult<JournalEntry>.Fail($"Error retrieving entry: {ex.Message}");
            }
        }

        public async Task<ServiceResult<JournalEntry>> GetEntryByIdAsync(int id)
        {
            try
            {
                var entry = await _context.JournalEntries
                    .Include(e => e.Tags)
                    .FirstOrDefaultAsync(e => e.Id == id);

                return entry != null
                    ? ServiceResult<JournalEntry>.Ok(entry)
                    : ServiceResult<JournalEntry>.Fail("Entry not found.");
            }
            catch (Exception ex)
            {
                return ServiceResult<JournalEntry>.Fail($"Error retrieving entry: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<JournalEntry>>> GetEntriesAsync(int page = 1, int pageSize = 10)
        {
            try
            {
                var entries = await _context.JournalEntries
                    .Include(e => e.Tags)
                    .OrderByDescending(e => e.Date)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return ServiceResult<List<JournalEntry>>.Ok(entries);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<JournalEntry>>.Fail($"Error retrieving entries: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<JournalEntry>>> SearchEntriesAsync(
            string? searchText, DateTime? startDate, DateTime? endDate, Mood? mood, List<int>? tagIds)
        {
            try
            {
                var query = _context.JournalEntries.Include(e => e.Tags).AsQueryable();

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    query = query.Where(e => e.Title.Contains(searchText) || e.Content.Contains(searchText));
                }

                if (startDate.HasValue)
                {
                    query = query.Where(e => e.Date >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(e => e.Date <= endDate.Value);
                }

                if (mood.HasValue)
                {
                    query = query.Where(e => e.PrimaryMood == mood.Value);
                }

                if (tagIds != null && tagIds.Any())
                {
                    query = query.Where(e => e.Tags.Any(t => tagIds.Contains(t.Id)));
                }

                var entries = await query.OrderByDescending(e => e.Date).ToListAsync();
                return ServiceResult<List<JournalEntry>>.Ok(entries);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<JournalEntry>>.Fail($"Error searching entries: {ex.Message}");
            }
        }

        public async Task<ServiceResult<JournalEntry>> CreateEntryAsync(JournalEntryViewModel model)
        {
            try
            {
                // Check if entry already exists for this date
                var existingEntry = await _context.JournalEntries
                    .FirstOrDefaultAsync(e => e.Date.Date == model.Date.Date);

                if (existingEntry != null)
                {
                    return ServiceResult<JournalEntry>.Fail("An entry already exists for this date. Please edit the existing entry.");
                }

                var entry = new JournalEntry
                {
                    Title = model.Title,
                    Content = model.Content,
                    Date = model.Date,
                    PrimaryMood = model.PrimaryMood,
                    SecondaryMood1 = model.SecondaryMoods.Count > 0 ? model.SecondaryMoods[0] : null,
                    SecondaryMood2 = model.SecondaryMoods.Count > 1 ? model.SecondaryMoods[1] : null,
                    Category = model.Category,
                    IsFavorite = model.IsFavorite,
                    CreatedAt = DateTime.UtcNow
                };

                // Add tags
                if (model.TagIds.Any())
                {
                    var tags = await _context.Tags.Where(t => model.TagIds.Contains(t.Id)).ToListAsync();
                    entry.Tags = tags;
                }

                _context.JournalEntries.Add(entry);
                await _context.SaveChangesAsync();

                return ServiceResult<JournalEntry>.Ok(entry);
            }
            catch (Exception ex)
            {
                return ServiceResult<JournalEntry>.Fail($"Error creating entry: {ex.Message}");
            }
        }

        public async Task<ServiceResult<JournalEntry>> UpdateEntryAsync(JournalEntryViewModel model)
        {
            try
            {
                var entry = await _context.JournalEntries
                    .Include(e => e.Tags)
                    .FirstOrDefaultAsync(e => e.Id == model.Id);

                if (entry == null)
                {
                    return ServiceResult<JournalEntry>.Fail("Entry not found.");
                }

                // Prevent updating to a date that already has an entry (other than this one)
                var duplicateDateEntry = await _context.JournalEntries
                    .FirstOrDefaultAsync(e => e.Date.Date == model.Date.Date && e.Id != model.Id);
                if (duplicateDateEntry != null)
                {
                    return ServiceResult<JournalEntry>.Fail("Another entry already exists for this date. Only one entry per day is allowed.");
                }

                entry.Title = model.Title;
                entry.Content = model.Content;
                entry.PrimaryMood = model.PrimaryMood;
                entry.SecondaryMood1 = model.SecondaryMoods.Count > 0 ? model.SecondaryMoods[0] : null;
                entry.SecondaryMood2 = model.SecondaryMoods.Count > 1 ? model.SecondaryMoods[1] : null;
                entry.Category = model.Category;
                entry.IsFavorite = model.IsFavorite;
                entry.Date = model.Date;
                entry.UpdatedAt = DateTime.UtcNow;

                // Update tags
                entry.Tags.Clear();
                if (model.TagIds.Any())
                {
                    var tags = await _context.Tags.Where(t => model.TagIds.Contains(t.Id)).ToListAsync();
                    foreach (var tag in tags)
                    {
                        entry.Tags.Add(tag);
                    }
                }

                await _context.SaveChangesAsync();
                return ServiceResult<JournalEntry>.Ok(entry);
            }
            catch (Exception ex)
            {
                return ServiceResult<JournalEntry>.Fail($"Error updating entry: {ex.Message}");
            }
        }

        public async Task<ServiceResult> DeleteEntryAsync(int id)
        {
            try
            {
                var entry = await _context.JournalEntries.FindAsync(id);
                if (entry == null)
                {
                    return ServiceResult.Fail("Entry not found.");
                }

                _context.JournalEntries.Remove(entry);
                await _context.SaveChangesAsync();
                return ServiceResult.Ok();
            }
            catch (Exception ex)
            {
                return ServiceResult.Fail($"Error deleting entry: {ex.Message}");
            }
        }

        public async Task<ServiceResult<MoodAnalyticsModel>> GetAnalyticsAsync()
        {
            try
            {
                var entries = await _context.JournalEntries.Include(e => e.Tags).ToListAsync();

                var analytics = new MoodAnalyticsModel
                {
                    TotalEntries = entries.Count,
                    MoodDistribution = entries.GroupBy(e => e.PrimaryMood.ToString())
                        .ToDictionary(g => g.Key, g => g.Count()),
                    TagUsage = entries.SelectMany(e => e.Tags)
                        .GroupBy(t => t.Name)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    AverageWordCount = entries.Any()
                        ? entries.Average(e => e.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length)
                        : 0,
                    WordCountTrends = entries.OrderBy(e => e.Date)
                        .Select(e => new WordCountTrend
                        {
                            Date = e.Date,
                            WordCount = e.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length
                        }).ToList()
                };

                if (analytics.MoodDistribution.Any())
                {
                    analytics.MostFrequentMood = analytics.MoodDistribution.OrderByDescending(x => x.Value).First().Key;
                }

                if (analytics.TagUsage.Any())
                {
                    analytics.MostUsedTag = analytics.TagUsage.OrderByDescending(x => x.Value).First().Key;
                }

                // Calculate streaks
                var streakResult = await GetStreakAsync();
                if (streakResult.Success && streakResult.Data != default)
                {
                    analytics.CurrentStreak = streakResult.Data.current;
                    analytics.LongestStreak = streakResult.Data.longest;
                }

                return ServiceResult<MoodAnalyticsModel>.Ok(analytics);
            }
            catch (Exception ex)
            {
                return ServiceResult<MoodAnalyticsModel>.Fail($"Error getting analytics: {ex.Message}");
            }
        }

        public async Task<ServiceResult<(int current, int longest)>> GetStreakAsync()
        {
            try
            {
                var dates = await _context.JournalEntries
                    .Select(e => e.Date.Date)
                    .Distinct()
                    .OrderByDescending(d => d)
                    .ToListAsync();

                if (!dates.Any())
                {
                    return ServiceResult<(int, int)>.Ok((0, 0));
                }

                int currentStreak = 0;
                int longestStreak = 0;
                int tempStreak = 1;

                // Calculate current streak
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

                // Calculate longest streak
                for (int i = 1; i < dates.Count; i++)
                {
                    if ((dates[i - 1] - dates[i]).Days == 1)
                    {
                        tempStreak++;
                    }
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
                return ServiceResult<(int, int)>.Fail($"Error calculating streak: {ex.Message}");
            }
        }
    }
}
