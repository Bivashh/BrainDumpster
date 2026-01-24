using System.ComponentModel.DataAnnotations;

namespace BrainDumpster.Model;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Username { get; set; } = "";

    [Required]
    public string Pin { get; set; }
}
