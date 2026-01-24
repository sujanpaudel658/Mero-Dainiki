namespace Mero_Dainiki.Entities
{
    /// <summary>
    /// Journal entry entity - only one entry per day allowed
    /// </summary>
    public class JournalEntry
    {
        public int Id { get; set; }
        public int UserId { get; set; }  // Link to user
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Today;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Mood tracking - primary mood required, secondary moods optional (up to 2)
        public Mood PrimaryMood { get; set; } = Mood.Neutral;
        public Mood? SecondaryMood1 { get; set; }
        public Mood? SecondaryMood2 { get; set; }
        
        public EntryCategory Category { get; set; } = EntryCategory.Personal;
        public bool IsFavorite { get; set; } = false;
        public string? ImagePath { get; set; }
        
        // Word count for analytics
        public int WordCount => Content?.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length ?? 0;
        
        // Navigation property for tags
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();
    }
}
