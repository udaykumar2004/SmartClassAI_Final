// =========================================================
// FILE: Models/ViewModels/StudentManagementVM.cs
// =========================================================

namespace SmartClassAI.Models.ViewModels
{
    public class StudentManagementVM
    {
        public int MemberId { get; set; }

        public int ClassroomId { get; set; }

        public string ClassroomName { get; set; } = string.Empty;

        public string StudentId { get; set; } = string.Empty;

        public string StudentName { get; set; } = string.Empty;

        public string StudentEmail { get; set; } = string.Empty;

        public string? ProfilePicUrl { get; set; }

        public DateTime JoinedAt { get; set; }

        public bool IsActive { get; set; }

        public int SubmissionCount { get; set; }
    }
}
