namespace Mero_Dainiki.Models
{
    /// <summary>
    /// Model for mood analytics and dashboard
    /// </summary>
    public class MoodAnalyticsModel
    {
        public int TotalEntries { get; set; }
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public int MissedDays { get; set; }
        public Dictionary<string, int> MoodDistribution { get; set; } = new();
        public string MostFrequentMood { get; set; } = string.Empty;
        public Dictionary<string, int> TagUsage { get; set; } = new();
        public string MostUsedTag { get; set; } = string.Empty;
        public List<WordCountTrend> WordCountTrends { get; set; } = new();
        public double AverageWordCount { get; set; }
    }

    /// <summary>
    /// Word count trend data point
    /// </summary>
    public class WordCountTrend
    {
        public DateTime Date { get; set; }
        public int WordCount { get; set; }
    }
}
