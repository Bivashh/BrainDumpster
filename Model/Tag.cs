using System.ComponentModel.DataAnnotations;

namespace BrainDumpster.Model;

public class Tag
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
