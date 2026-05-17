using System.ComponentModel.DataAnnotations;

namespace SmartClassAI.Models;

public class Enrollment
{
    public int Id { get; set; }

    // =========================
    // JOIN DATE
    // =========================

    public DateTime JoinedAt
    {
        get;
        set;
    } = DateTime.UtcNow;

    // =========================
    // CLASSROOM RELATION
    // =========================

    [Required]
    public int ClassroomId
    {
        get;
        set;
    }

    public Classroom? Classroom
    {
        get;
        set;
    }

    // =========================
    // STUDENT RELATION
    // =========================

    [Required]
    public string StudentId
    {
        get;
        set;
    } = string.Empty;

    public ApplicationUser? Student
    {
        get;
        set;
    }
}