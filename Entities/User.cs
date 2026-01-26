namespace Mero_Dainiki.Entities
{
    /// <summary>
    /// User entity for authentication and journal ownership
    /// Every user has isolated data - own entries, tags, and PIN
    /// </summary>
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? Pin { get; set; } // Per-user PIN protection
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation properties for data isolation
        public ICollection<JournalEntry> JournalEntries { get; set; } = new List<JournalEntry>();
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();
        public ICollection<LoginHistory> LoginHistories { get; set; } = new List<LoginHistory>();
    }
}
