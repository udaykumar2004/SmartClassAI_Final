// ============================================================
// ClassroomDetailsVM.cs — UPDATED
//
// CHANGES:
//  1. Added IsClassTeacher, IsAdmin, IsEnrolledStudent flags
//     so views don't need to re-query UserManager
//  2. Added CanManage, CanPost helper properties
//  3. Students list now comes from Members nav property (fixed in controller)
// ============================================================

using SmartClassAI.Models;

namespace SmartClassAI.Models.ViewModels
{
    public class ClassroomDetailsVM
    {
        // =============================================
        // MAIN DATA
        // =============================================
        public Classroom Classroom { get; set; } = null!;

        // =============================================
        // USER CONTEXT (set by controller)
        // =============================================
        public bool IsAdmin { get; set; }
        public bool IsClassTeacher { get; set; }
        public bool IsEnrolledStudent { get; set; }

        // Computed helpers
        public bool CanManageClassroom => IsAdmin || IsClassTeacher;
        public bool CanPost => IsAdmin || IsClassTeacher;
        public bool CanSubmit => IsEnrolledStudent;
        public bool CanStartMeeting => IsAdmin || IsClassTeacher;
        public bool CanJoinMeeting => IsAdmin || IsClassTeacher || IsEnrolledStudent;

        // =============================================
        // COUNTS
        // =============================================
        public int StudentCount { get; set; }
        public int AssignmentCount { get; set; }
        public int AnnouncementCount { get; set; }
        public int NotesCount { get; set; }
        public int MaterialsCount { get; set; }
        public int MeetingsCount { get; set; }

        // =============================================
        // STATUS
        // =============================================
        public bool IsLive { get; set; }

        // =============================================
        // LISTS
        // =============================================
        public List<ApplicationUser> Students { get; set; } = new();
        public List<Assignment> Assignments { get; set; } = new();
        public List<Announcement> Announcements { get; set; } = new();
        public List<Material> Materials { get; set; } = new();
        public List<Meeting> Meetings { get; set; } = new();
        public List<Note> Notes { get; set; } = new();
    }
}