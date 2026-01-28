using BrainDumpster.Data;
using BrainDumpster.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace BrainDumpster.Services
{
    public class JournalService
    {
        private readonly AppDbContext _db;
        private readonly IJSRuntime _jsRuntime;

        public JournalService(AppDbContext db, IJSRuntime jsRuntime)
        {
            _db = db;
            _jsRuntime = jsRuntime;
        }

        // Get logged in user ID
        public async Task<int?> GetLoggedUserIdAsync()
        {
            var userIdStr = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "loggedUserId");
            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
                return userId;
            return null;
        }

        // Get user's entries
        public List<JournalEntry> GetEntries(int userId)
        {
            return _db.JournalEntries
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.EntryDate)
                .ToList();
        }

        // Get single entry by ID
        public JournalEntry? GetEntryById(int entryId, int userId)
        {
            return _db.JournalEntries
                .Include(e => e.Tags)
                .FirstOrDefault(e => e.Id == entryId && e.UserId == userId);
        }

        // Save entry (NEW - for multiple entries per day)
        public async Task<(bool Success, string Message)> SaveEntryAsync(
            int userId, string content, string mood, List<string> tags)
        {
            if (string.IsNullOrWhiteSpace(content) || content == "<p><br></p>")
                return (false, "Please write something!");

            var entry = new JournalEntry
            {
                UserId = userId,
                Content = content,
                PrimaryMood = mood,
                EntryDate = DateTime.Now,
                Tags = tags.Select(t => new Tag { Name = t }).ToList()
            };

            _db.JournalEntries.Add(entry);
            await _db.SaveChangesAsync();

            return (true, "Entry saved successfully!");
        }

        // Update existing entry
        public async Task<(bool Success, string Message)> UpdateEntryAsync(
            int entryId, int userId, string content, string mood, List<string> tags)
        {
            var entry = _db.JournalEntries
                .Include(e => e.Tags)
                .FirstOrDefault(e => e.Id == entryId && e.UserId == userId);

            if (entry == null)
                return (false, "Entry not found!");

            entry.Content = content;
            entry.PrimaryMood = mood;
            entry.EntryDate = DateTime.Now; // Update timestamp when editing

            // Clear existing tags
            _db.Tags.RemoveRange(entry.Tags);

            // Add new tags
            entry.Tags = tags.Select(t => new Tag { Name = t }).ToList();

            await _db.SaveChangesAsync();
            return (true, "Entry updated successfully!");
        }

        // Save or update entry (with one-entry-per-day restriction)
        public async Task<(bool Success, string Message)> SaveEntryOncePerDayAsync(
    int userId, string content, string mood, List<string> tags)
        {
            if (string.IsNullOrWhiteSpace(content) || content == "<p><br></p>")
                return (false, "Please write something!");

            var today = DateTime.Today;

            var existingEntry = _db.JournalEntries
                .FirstOrDefault(e =>
                    e.UserId == userId &&
                    e.EntryDate.Date == today);

            if (existingEntry != null)
            {
                return (false, "You have already entered today's journal. Please edit it or wait till tomorrow.");
            }

            var entry = new JournalEntry
            {
                UserId = userId,
                Content = content,
                PrimaryMood = mood,
                EntryDate = DateTime.Now,
                Tags = tags.Select(t => new Tag { Name = t }).ToList()
            };

            _db.JournalEntries.Add(entry);
            await _db.SaveChangesAsync();

            return (true, "Entry saved successfully!");
        }



        public async Task DeleteEntryAsync(int entryId, int userId)
        {
            var entry = _db.JournalEntries
                .Include(e => e.Tags)
                .FirstOrDefault(e => e.Id == entryId && e.UserId == userId);

            if (entry == null) return;

            // delete tags first
            if (entry.Tags != null && entry.Tags.Any())
            {
                _db.Tags.RemoveRange(entry.Tags);
            }

            _db.JournalEntries.Remove(entry);
            await _db.SaveChangesAsync();
        }

        // Quill editor methods
        public async Task InitEditor() => await _jsRuntime.InvokeVoidAsync("initQuill", "editor");
        public async Task<string> GetEditorContent() => await _jsRuntime.InvokeAsync<string>("getQuillHtml");
        public async Task ClearEditor() => await _jsRuntime.InvokeVoidAsync("clearQuill");
        public async Task SetEditorContent(string html) => await _jsRuntime.InvokeVoidAsync("setQuillHtml", "editor", html);
    }
}