using Microsoft.EntityFrameworkCore;
using Mero_Dainiki.Entities;

namespace Mero_Dainiki.Data
{
    /// <summary>
    /// EF Core SQLite Database Context for the Journal App
    /// Implements complete user isolation with UserId filtering on all entities
    /// </summary>
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<JournalEntry> JournalEntries { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<LoginHistory> LoginHistories { get; set; }

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

            // User entity configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(255);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Pin).HasMaxLength(50);
                entity.Property(e => e.CreatedAt).HasDefaultValue(DateTime.UtcNow);
                entity.Property(e => e.IsActive).HasDefaultValue(true);

                // Unique constraints
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();

                // User has many entries, tags, and login histories
                entity.HasMany(u => u.JournalEntries)
                    .WithOne()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(u => u.Tags)
                    .WithOne()
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(u => u.LoginHistories)
                    .WithOne()
                    .HasForeignKey(h => h.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // JournalEntry entity configuration - per-user entries
            modelBuilder.Entity<JournalEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired(); // CRITICAL: Every entry belongs to a user
                entity.Property(e => e.Title).HasMaxLength(200);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.Date).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValue(DateTime.UtcNow);
                entity.Property(e => e.IsFavorite).HasDefaultValue(false);

                // Indexes for fast queries by user
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => new { e.UserId, e.Date }).IsUnique(); // One entry per day per user
            });

            // Tag entity configuration - per-user tags
            modelBuilder.Entity<Tag>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired(); // CRITICAL: Every tag belongs to a user
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.Property(e => e.Color).HasMaxLength(10);
                entity.Property(e => e.CreatedAt).HasDefaultValue(DateTime.UtcNow);

                // Unique tag name per user
                entity.HasIndex(e => new { e.UserId, e.Name }).IsUnique();

                // Many-to-many relationship: JournalEntry <-> Tag
                entity.HasMany(t => t.JournalEntries)
                    .WithMany(e => e.Tags)
                    .UsingEntity("JournalEntryTag");
            });

            // LoginHistory entity configuration - audit trail
            modelBuilder.Entity<LoginHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.LoginTime)
                    .IsRequired()
                    .HasDefaultValue(DateTime.UtcNow);
                entity.Property(e => e.IsSuccessful).HasDefaultValue(true);
                entity.Property(e => e.IpAddress).HasMaxLength(50);
                entity.Property(e => e.DeviceInfo).HasMaxLength(255);

                // Indexes for audit queries
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.LoginTime);
                entity.HasIndex(e => new { e.UserId, e.LoginTime });
            });
        }
    }
}
