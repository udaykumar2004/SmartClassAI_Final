namespace SmartClassAI.Models
{
    public class LiveClass
    {
        public int Id { get; set; }

        // =========================
        // CLASSROOM
        // =========================

        public int ClassroomId { get; set; }

        public Classroom Classroom { get; set; }

        // =========================
        // TEACHER
        // =========================

        public string TeacherId { get; set; }

        public ApplicationUser Teacher { get; set; }

        // =========================
        // MEETING
        // =========================

        public string MeetingLink { get; set; }

        public bool IsLive { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime? EndedAt { get; set; }
    }
}