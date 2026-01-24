using Microsoft.EntityFrameworkCore;
using Mero_Dainiki.Entities;

namespace Mero_Dainiki.Data
{
    /// <summary>
    /// EF Core SQLite Database Context for the Journal App
    /// </summary>
    public class AppDbContext : DbContext
    {
        public DbSet<JournalEntry> JournalEntries { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<User> Users { get; set; }

        private readonly string _dbPath;

        public AppDbContext()
        {
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _dbPath = Path.Combine(folder, "merodainiki.db");
        }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _dbPath = Path.Combine(folder, "merodainiki.db");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite($"Data Source={_dbPath}");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // JournalEntry configuration
            modelBuilder.Entity<JournalEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(200);
                entity.Property(e => e.Content).IsRequired();
                entity.HasIndex(e => e.Date);
            });

            // Tag configuration
            modelBuilder.Entity<Tag>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).HasMaxLength(100);
            });

            // Many-to-many relationship: JournalEntry <-> Tag
            modelBuilder.Entity<JournalEntry>()
                .HasMany(e => e.Tags)
                .WithMany(t => t.JournalEntries);
        }
    }
}
