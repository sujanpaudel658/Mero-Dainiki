namespace Mero_Dainiki.Entities
{
    /// <summary>
    /// Tag entity - per-user tags for journal entry categorization
    /// </summary>
    public class Tag
    {
        public int Id { get; set; }
        public int UserId { get; set; } // Each tag belongs to a specific user
        public string Name { get; set; } = string.Empty;
        public string? Color { get; set; } = "#6366f1"; // Default primary color
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public ICollection<JournalEntry> JournalEntries { get; set; } = new List<JournalEntry>();
    }
}
