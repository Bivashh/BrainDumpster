// Data/AppDbContext.cs
using BrainDumpster.Model;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace BrainDumpster.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<JournalEntry> JournalEntries { get; set; }
        public DbSet<Tag> Tags { get; set; }

        // Add a constructor if you don't have one
        public AppDbContext()
        {
            // Initialize the database
            try
            {
                Database.EnsureCreated();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Database initialization error: {ex.Message}");
                // Don't throw here - let it fail gracefully
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Configure SQLite for MAUI
            string databasePath = Path.Combine(FileSystem.AppDataDirectory, "braindumpster.db");
            optionsBuilder.UseSqlite($"Data Source={databasePath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure your model relationships here
            base.OnModelCreating(modelBuilder);
        }

        // Optional: Add this method to call from your pages
        public void EnsureDatabase()
        {
            try
            {
                Database.EnsureCreated();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EnsureDatabase error: {ex.Message}");
            }
        }
    }
}