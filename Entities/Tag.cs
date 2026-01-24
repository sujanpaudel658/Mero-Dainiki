namespace Mero_Dainiki.Entities
{
    public class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Color { get; set; } = "#6366f1"; // Default primary color
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property
        public ICollection<JournalEntry> JournalEntries { get; set; } = new List<JournalEntry>();
    }
}
