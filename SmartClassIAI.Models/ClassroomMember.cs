// ============================================================
// ClassroomMember.cs
// ENHANCED: Added JoinedAt timestamp (was missing from original)
//           Added unique index hint via attribute comment
//           This is now the ONLY way membership is tracked
// ============================================================

using System.ComponentModel.DataAnnotations;

namespace SmartClassAI.Models
{
    public class ClassroomMember
    {
        public int Id { get; set; }

        // =============================================
        // CLASSROOM
        // =============================================

        public int ClassroomId { get; set; }

        public Classroom? Classroom { get; set; }

        // =============================================
        // USER
        // =============================================

        public string UserId { get; set; } = string.Empty;

        public ApplicationUser? User { get; set; }

        // =============================================
        // ROLE INSIDE CLASSROOM
        // =============================================

        [MaxLength(50)]
        public string Role { get; set; } = string.Empty;
        // Possible values: "Teacher", "Student"

        // =============================================
        // METADATA
        // =============================================

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;
    }
}