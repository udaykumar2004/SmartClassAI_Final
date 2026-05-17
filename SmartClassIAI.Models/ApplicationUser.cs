// ============================================================
// ApplicationUser.cs
// FIX: Removed ICollection<Enrollment> nav property
//      Added ICollection<ClassroomMember> nav property
//      This is the single membership source of truth
// ============================================================

using Microsoft.AspNetCore.Identity;

namespace SmartClassAI.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public string? ProfilePictureUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsSuspended { get; set; } = false;

    public string? SuspensionReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    // ===================================================
    // NAVIGATION PROPERTIES
    // ===================================================

    // Classrooms this user CREATED (as teacher)
    public ICollection<Classroom> CreatedClasses { get; set; }
        = new List<Classroom>();

    // Classrooms this user is a MEMBER of (teacher OR student)
    // FIX: Previously was ICollection<Enrollment> which broke
    //      all membership checks for ClassroomMember-joined students
    public ICollection<ClassroomMember> ClassroomMemberships { get; set; }
        = new List<ClassroomMember>();

    // Submissions
    public ICollection<AssignmentSubmission> AssignmentSubmissions { get; set; }
        = new List<AssignmentSubmission>();
}