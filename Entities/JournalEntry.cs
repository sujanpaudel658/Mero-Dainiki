namespace Mero_Dainiki.Entities
{
    /// <summary>
    /// Journal entry entity - only one entry per day allowed
    /// </summary>
    public class JournalEntry
    {
        public int Id { get; set; }
        public int UserId { get; set; }  // Ownership: Link to user table
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty; // Rich/Markdown content string
        public DateTime Date { get; set; } = DateTime.Today; // Source-of-truth for historical logs
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Emotional tracking properties 
        public Mood PrimaryMood { get; set; } = Mood.Neutral; // High-level primary feeling
        public Mood? SecondaryMood1 { get; set; } // Detailed emotional nuance 1
        public Mood? SecondaryMood2 { get; set; } // Detailed emotional nuance 2
        
        public EntryCategory Category { get; set; } = EntryCategory.Personal;
        public bool IsFavorite { get; set; } = false;
        public string? ImagePath { get; set; }
        
        // Calculated property for writing metrics 
        public int WordCount => Content?.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length ?? 0;
        
        // Relationship mapping 
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();
    }
}
