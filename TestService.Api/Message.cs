using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TestService;

[Table("Messages")]
public class Message
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Text { get; set; } = default!;
}