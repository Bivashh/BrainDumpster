using BrainDumpster.Data;
using BrainDumpster.Model;
using Microsoft.JSInterop;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BrainDumpster.Services
{
    public class SettingsService
    {
        private readonly AppDbContext _db;
        private readonly IJSRuntime _jsRuntime;

        public SettingsService(AppDbContext db, IJSRuntime jsRuntime)
        {
            _db = db;
            _jsRuntime = jsRuntime;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        // ========== EXISTING METHODS (KEEP THESE) ==========

        // 1. Get current user
        public async Task<User?> GetUserAsync(int userId)
        {
            return await _db.Users.FindAsync(userId);
        }

        // 2. Update username
        public async Task<(bool Success, string Message)> UpdateUsernameAsync(int userId, string newUsername)
        {
            try
            {
                var user = await _db.Users.FindAsync(userId);
                if (user == null) return (false, "User not found");

                if (string.IsNullOrWhiteSpace(newUsername) || newUsername.Length < 3)
                    return (false, "Username must be at least 3 characters");

                var existing = _db.Users.FirstOrDefault(u => u.Username == newUsername && u.Id != userId);
                if (existing != null)
                    return (false, "Username already taken");

                user.Username = newUsername;
                await _db.SaveChangesAsync();

                return (true, "Username updated!");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        // 3. Update PIN
        public async Task<(bool Success, string Message)> UpdatePinAsync(int userId, string currentPin, string newPin)
        {
            try
            {
                var user = await _db.Users.FindAsync(userId);
                if (user == null) return (false, "User not found");

                if (user.Pin != currentPin)
                    return (false, "Current PIN is incorrect");

                if (string.IsNullOrEmpty(newPin) || newPin.Length < 4 || !newPin.All(char.IsDigit))
                    return (false, "New PIN must be 4-6 digits");

                user.Pin = newPin;
                await _db.SaveChangesAsync();

                return (true, "PIN changed!");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        // 4. Get user stats
        public async Task<(int TotalEntries, int Streak, string MemberSince)> GetUserStatsAsync(int userId)
        {
            try
            {
                var user = await _db.Users.FindAsync(userId);
                if (user == null) return (0, 0, "");

                int totalEntries = _db.JournalEntries.Count(e => e.UserId == userId);
                int streak = CalculateStreak(userId);
                string memberSince = GetMemberSince(user);

                return (totalEntries, streak, memberSince);
            }
            catch
            {
                return (0, 0, "");
            }
        }

        // ========== PDF EXPORT METHODS (NEW) ==========

        // 5. Export ALL journals as PDF
        public async Task<(bool Success, string Message, byte[]? PdfData)> ExportPdfAllAsync(int userId)
        {
            try
            {
                var user = await _db.Users.FindAsync(userId);
                if (user == null) return (false, "User not found", null);

                var entries = _db.JournalEntries
                    .Where(e => e.UserId == userId)
                    .OrderByDescending(e => e.EntryDate)
                    .ToList();

                var pdfBytes = BuildJournalPdf(user.Username, entries, "All Journals");
                return (true, "PDF generated", pdfBytes);
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}", null);
            }
        }

        // 6. Export by specific date as PDF
        public async Task<(bool Success, string Message, byte[]? PdfData)> ExportPdfByDateAsync(int userId, DateTime date)
        {
            try
            {
                var user = await _db.Users.FindAsync(userId);
                if (user == null) return (false, "User not found", null);

                var start = date.Date;
                var end = date.Date.AddDays(1).AddSeconds(-1);

                var entries = _db.JournalEntries
                    .Where(e => e.UserId == userId && e.EntryDate >= start && e.EntryDate <= end)
                    .OrderByDescending(e => e.EntryDate)
                    .ToList();

                if (!entries.Any())
                    return (false, $"No entries found for {date:MMMM dd, yyyy}", null);

                var pdfBytes = BuildJournalPdf(user.Username, entries, $"Journals for {date:MMMM dd, yyyy}");
                return (true, "PDF generated", pdfBytes);
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}", null);
            }
        }

        // 7. Export by date range as PDF
        public async Task<(bool Success, string Message, byte[]? PdfData)> ExportPdfByDateRangeAsync(int userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var user = await _db.Users.FindAsync(userId);
                if (user == null) return (false, "User not found", null);

                var start = startDate.Date;
                var end = endDate.Date.AddDays(1).AddSeconds(-1);

                var entries = _db.JournalEntries
                    .Where(e => e.UserId == userId && e.EntryDate >= start && e.EntryDate <= end)
                    .OrderByDescending(e => e.EntryDate)
                    .ToList();

                if (!entries.Any())
                    return (false, $"No entries found for {startDate:MMM dd} - {endDate:MMM dd, yyyy}", null);

                var pdfBytes = BuildJournalPdf(user.Username, entries, $"Journals from {startDate:MMM dd} to {endDate:MMM dd, yyyy}");
                return (true, "PDF generated", pdfBytes);
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}", null);
            }
        }

        // 8. Build PDF document
        public byte[] BuildJournalPdf(string username, List<JournalEntry> entries, string title)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    // Header
                    page.Header().Column(col =>
                    {
                        col.Item().Text(title).FontSize(18).Bold();
                        col.Item().Text($"User: {username}")
                            .FontSize(10)
                            .FontColor(QuestPDF.Helpers.Colors.Grey.Darken2);

                        col.Item().Text($"Generated: {DateTime.Now:dddd, MMM dd yyyy HH:mm}")
                            .FontSize(10)
                            .FontColor(QuestPDF.Helpers.Colors.Grey.Darken2);

                        col.Item().LineHorizontal(1).LineColor(QuestPDF.Helpers.Colors.Grey.Lighten2);
                    });

                    // Content
                    page.Content().PaddingTop(10).Column(col =>
                    {
                        if (entries == null || entries.Count == 0)
                        {
                            col.Item().Text("No journal entries found.")
                                .Italic()
                                .FontColor(QuestPDF.Helpers.Colors.Grey.Darken1);
                            return;
                        }

                        foreach (var entry in entries)
                        {
                            col.Item().Element(card =>
                            {
                                card.Border(1)
                                    .BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2)
                                    .Padding(12)
                                    .Column(c =>
                                    {
                                        // Date and Mood
                                        c.Item().Text($"{entry.EntryDate:dddd, MMMM dd, yyyy hh:mm tt}")
                                            .Bold()
                                            .FontSize(12);

                                        c.Item().Text($"Mood: {entry.PrimaryMood}")
                                            .FontSize(10)
                                            .FontColor(QuestPDF.Helpers.Colors.Grey.Darken2);

                                        // Tags if available
                                        if (entry.Tags != null && entry.Tags.Any())
                                        {
                                            c.Item().Text($"Tags: {string.Join(", ", entry.Tags.Select(t => t.Name))}")
                                                .FontSize(10)
                                                .FontColor(QuestPDF.Helpers.Colors.Blue.Medium);
                                        }

                                        c.Item().PaddingTop(6).LineHorizontal(1).LineColor(QuestPDF.Helpers.Colors.Grey.Lighten2);

                                        // Content
                                        var plainContent = HtmlToPlainText(entry.Content);
                                        if (string.IsNullOrWhiteSpace(plainContent))
                                            plainContent = "(no content)";

                                        c.Item().PaddingTop(6).Text(plainContent);
                                    });
                            });

                            col.Item().PaddingBottom(10);
                        }
                    });

                    // Footer
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        // 9. Download PDF file
        public async Task DownloadPdfFile(byte[] pdfData, string fileName)
        {
            try
            {
                var base64 = Convert.ToBase64String(pdfData);
                await _jsRuntime.InvokeVoidAsync("downloadPdf", base64, fileName);
            }
            catch (Exception ex)
            {
                throw new Exception($"Download failed: {ex.Message}");
            }
        }

        // ========== HELPER METHODS ==========

        private string HtmlToPlainText(string html)
        {
            var s = (html ?? "").Trim();
            if (string.IsNullOrWhiteSpace(s)) return "";

            // Replace common HTML tags
            s = Regex.Replace(s, @"<\s*br\s*/?\s*>", "\n", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"</\s*p\s*>", "\n\n", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"</\s*div\s*>", "\n", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"</\s*li\s*>", "\n", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"<\s*li[^>]*>", "• ", RegexOptions.IgnoreCase);

            // Remove all other tags
            s = Regex.Replace(s, "<.*?>", string.Empty);

            // Decode HTML entities
            s = System.Net.WebUtility.HtmlDecode(s);

            // Clean up
            s = s.Replace("\r", "");
            s = Regex.Replace(s, @"\n{3,}", "\n\n");
            s = Regex.Replace(s, @"[ \t]{2,}", " ");

            return s.Trim();
        }

        private int CalculateStreak(int userId)
        {
            try
            {
                var entryDates = _db.JournalEntries
                    .Where(e => e.UserId == userId)
                    .Select(e => e.EntryDate.Date)
                    .Distinct()
                    .OrderByDescending(d => d)
                    .ToList();

                if (!entryDates.Any()) return 0;

                int streak = 0;
                var checkDate = DateTime.Today;

                if (entryDates.Contains(DateTime.Today))
                {
                    streak = 1;
                    checkDate = DateTime.Today.AddDays(-1);

                    while (entryDates.Contains(checkDate))
                    {
                        streak++;
                        checkDate = checkDate.AddDays(-1);
                    }
                }

                return streak;
            }
            catch
            {
                return 0;
            }
        }

        private string GetMemberSince(User user)
        {
            try
            {
                var createdAtProperty = typeof(User).GetProperty("CreatedAt");
                if (createdAtProperty != null)
                {
                    var value = createdAtProperty.GetValue(user);
                    if (value is DateTime createdAt)
                        return createdAt.ToString("MMM yyyy");
                }

                return "Recently";
            }
            catch
            {
                return "Recently";
            }
        }
    }
}