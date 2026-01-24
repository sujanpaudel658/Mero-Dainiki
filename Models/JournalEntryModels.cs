namespace Mero_Dainiki.Models
{
    /// <summary>
    /// ViewModel for creating/editing journal entries
    /// </summary>
    public class JournalEntryViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Today;
        public Entities.Mood PrimaryMood { get; set; } = Entities.Mood.Neutral;
        public List<Entities.Mood> SecondaryMoods { get; set; } = new();
        public Entities.EntryCategory Category { get; set; } = Entities.EntryCategory.Personal;
        public List<int> TagIds { get; set; } = new();
        public bool IsFavorite { get; set; } = false;
    }

    /// <summary>
    /// Display model for showing journal entries in UI
    /// </summary>
    public class JournalEntryDisplayModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ContentPreview { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string MoodEmoji { get; set; } = "😐";
        public string MoodName { get; set; } = "Neutral";
        public string CategoryName { get; set; } = string.Empty;
        public List<string> TagNames { get; set; } = new();
        public bool IsFavorite { get; set; }
        public int WordCount { get; set; }
    }
}
