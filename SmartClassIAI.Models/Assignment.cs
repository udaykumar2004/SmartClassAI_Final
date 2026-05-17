using System.ComponentModel.DataAnnotations;

namespace SmartClassAI.Models;

public class Assignment
{
    public int Id { get; set; }

    // =========================
    // BASIC INFO
    // =========================

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(5000)]
    public string Description { get; set; } = string.Empty;

    // =========================
    // DATES
    // =========================

    [Display(Name = "Due Date")]
    public DateTime DueDate { get; set; }

    public DateTime CreatedAt { get; set; }
        = DateTime.UtcNow;

    // =========================
    // STATUS
    // =========================

    public string Status { get; set; }
        = "Published";

    // Draft
    // Published
    // Closed
    // Archived

    // =========================
    // FILE ATTACHMENT
    // =========================

    public string? FileUrl { get; set; }

    // =========================
    // CLASSROOM RELATION
    // =========================

    [Display(Name = "Classroom")]
    public int ClassroomId { get; set; }

    public Classroom? Classroom { get; set; }

    // =========================
    // TOTAL MARKS
    // =========================

    public int TotalMarks { get; set; } = 100;

    // =========================
    // SUBMISSION SETTINGS
    // =========================

    public bool AllowLateSubmission { get; set; }
        = true;

    public bool AllowComments { get; set; }
        = true;

    // =========================
    // REAL-TIME / TRACKING
    // =========================

    public int ViewCount { get; set; } = 0;

    public bool IsScheduled { get; set; } = false;

    public DateTime? ScheduledAt { get; set; }

    // =========================
    // NAVIGATION
    // =========================

    public ICollection<AssignmentSubmission> Submissions
    {
        get;
        set;
    } = new List<AssignmentSubmission>();

    public ICollection<AssignmentComment> Comments
    {
        get;
        set;
    } = new List<AssignmentComment>();
}