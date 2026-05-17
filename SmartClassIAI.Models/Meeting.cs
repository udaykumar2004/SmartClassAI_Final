using System;
using System.ComponentModel.DataAnnotations;

namespace SmartClassAI.Models
{
    public class Meeting
    {
        public int Id { get; set; }

        public int ClassroomId { get; set; }
        public Classroom? Classroom { get; set; }

        // FK
        public string UserId { get; set; } = string.Empty;

        // Navigation
        public ApplicationUser? User { get; set; }

        [Required]
        public string MeetingRoomId { get; set; } = Guid.NewGuid().ToString();

        public bool IsActive { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime? EndedAt { get; set; }
    }
}