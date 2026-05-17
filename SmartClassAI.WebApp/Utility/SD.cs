// ============================================================
// SD.cs — Static Details (Role Constants & App-Wide Strings)
// FIX: Role names were "Admin User", "Teacher User", "Student User"
//      but DashboardService and _Layout checked for "Admin","Teacher","Student"
//      This caused every role check outside controllers to fail silently.
// ============================================================

namespace SmartClassAI.WebApp.Utility
{
    public static class SD
    {
        // ===================================================
        // ROLES — must match exactly what is seeded via Identity
        // ===================================================
        public const string Role_Admin = "Admin";
        public const string Role_Teacher = "Teacher";
        public const string Role_Student = "Student";

        // ===================================================
        // ASSIGNMENT STATUS
        // ===================================================
        public const string Assignment_Draft = "Draft";
        public const string Assignment_Published = "Published";
        public const string Assignment_Closed = "Closed";
        public const string Assignment_Archived = "Archived";

        // ===================================================
        // SUBMISSION STATUS
        // ===================================================
        public const string Submission_Submitted = "Submitted";
        public const string Submission_Late = "Late";
        public const string Submission_Reviewed = "Reviewed";
        public const string Submission_Resubmitted = "Resubmitted";

        // ===================================================
        // CLASSROOM MEMBER ROLES
        // ===================================================
        public const string ClassroomRole_Teacher = "Teacher";
        public const string ClassroomRole_Student = "Student";

        // ===================================================
        // FILE UPLOAD
        // ===================================================
        public const string Upload_Folder = "uploads";
        public const string SubmissionFile_Folder = "submissionfiles";
        public const string ProfilePic_Folder = "profilepics";
        public const long MaxFileSize_Bytes = 10 * 1024 * 1024; // 10 MB

        public static readonly string[] Allowed_Assignment_Extensions =
            { ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".zip", ".png", ".jpg", ".jpeg" };

        public static readonly string[] Allowed_Submission_Extensions =
            { ".pdf", ".doc", ".docx", ".zip", ".png", ".jpg", ".jpeg" };

        public static readonly string[] Allowed_Image_Extensions =
            { ".png", ".jpg", ".jpeg", ".gif", ".webp" };
    }
}