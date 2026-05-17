// ============================================================
// Classroom.cs
// FIX: Removed ICollection<Enrollment> nav property
//      Added ICollection<ClassroomMember> as the membership collection
//      Added CoverImageUrl, Description, IsArchived for enterprise features
// ============================================================

using SmartClassAI.Models;

namespace SmartClassAI.Models
{
    public class Classroom
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Section { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string JoinCode { get; set; }
            = Guid.NewGuid().ToString()[..6].ToUpper();

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;

        // =============================================
        // STATUS
        // =============================================

        public bool IsArchived { get; set; } = false;

        public DateTime? ArchivedAt { get; set; }

        // =============================================
        // LIVE CLASSROOM STATUS
        // =============================================

        public bool IsLive { get; set; } = false;

        public string? MeetingLink { get; set; }

        // =============================================
        // COVER IMAGE
        // =============================================

        public string? CoverImageUrl { get; set; }

        // =============================================
        // TEACHER
        // =============================================

        public string? TeacherId { get; set; }

        public ApplicationUser? Teacher { get; set; }

        // =============================================
        // NAVIGATION PROPERTIES
        // =============================================

        // FIX: Replaced Enrollments with ClassroomMembers
        // This is the SINGLE membership source of truth
        public ICollection<ClassroomMember> Members { get; set; }
            = new List<ClassroomMember>();

        public ICollection<Assignment> Assignments { get; set; }
            = new List<Assignment>();

        public ICollection<Announcement> Announcements { get; set; }
            = new List<Announcement>();

        public ICollection<Material> Materials { get; set; }
            = new List<Material>();

        public ICollection<Note> Notes { get; set; }
            = new List<Note>();

        public ICollection<Meeting> Meetings { get; set; }
            = new List<Meeting>();
    }
}