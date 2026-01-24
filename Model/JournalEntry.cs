using BrainDumpster.Model;
using System.ComponentModel.DataAnnotations;

namespace BrainDumpster.Model;

public class JournalEntry
{
    [Key]
    public int Id { get; set; }

    public string Title { get; set; } = "Journal Entry";

    public string Content { get; set; } = "";

    public string PrimaryMood { get; set; } = "🙂";

    public DateTime EntryDate { get; set; } = DateTime.Now;

    public int UserId { get; set; } // foreign key
    public User? User { get; set; }

    public List<Tag> Tags { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
