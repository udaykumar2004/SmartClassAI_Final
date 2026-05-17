// =========================================================
// FILE: Models/ViewModels/ClassroomStudentVM.cs
// =========================================================

namespace SmartClassAI.Models.ViewModels
{
    public class ClassroomStudentVM
    {
        public int MemberId { get; set; }

        public string StudentId { get; set; } = string.Empty;

        public string StudentName { get; set; } = string.Empty;

        public string StudentEmail { get; set; } = string.Empty;

        public string? ProfilePicUrl { get; set; }

        public DateTime JoinedAt { get; set; }

        public int SubmissionCount { get; set; }

        public int TotalAssignments { get; set; }

        public double? AverageMarks { get; set; }

        // Assignment participation %
        public double AssignmentParticipationPct { get; set; }

        public bool IsActive { get; set; }
    }
}