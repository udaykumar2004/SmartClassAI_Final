using System.ComponentModel.DataAnnotations;

namespace SmartClassAI.Models;

public class AssignmentComment
{
    public int Id { get; set; }

    // COMMENT

    [Required]
    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    // USER

    public string UserName { get; set; } = string.Empty;

    // DATE

    public DateTime CreatedAt { get; set; }
        = DateTime.UtcNow;

    // ASSIGNMENT RELATION

    public int AssignmentId { get; set; }

    public Assignment? Assignment { get; set; }
}