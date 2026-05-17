using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartClassAI.Models
{
    public class AssignmentSubmission
    {
        public int Id { get; set; }

        // =========================
        // Submission Text
        // =========================

        [MaxLength(5000)]
        public string SubmissionText { get; set; }
            = string.Empty;

        // =========================
        // Uploaded File
        // =========================

        public string? SubmissionFileUrl { get; set; }

        // =========================
        // Submission Time
        // =========================

        public DateTime SubmittedAt { get; set; }
            = DateTime.UtcNow;

        // =========================
        // Status
        // =========================

        public string Status { get; set; }
            = "Submitted";

        // =========================
        // Marks
        // =========================

        public int? Marks { get; set; }

        // =========================
        // Teacher Feedback
        // =========================

        [MaxLength(2000)]
        public string? Feedback { get; set; }

        // =========================
        // Assignment Relation
        // =========================

        public int AssignmentId { get; set; }

        [ForeignKey("AssignmentId")]
        public Assignment? Assignment { get; set; }

        // =========================
        // Student Relation
        // =========================

        public string StudentId { get; set; }
            = string.Empty;

        [ForeignKey("StudentId")]
        public ApplicationUser? Student { get; set; }
    }
}