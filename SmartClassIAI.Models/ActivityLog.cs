using System;

namespace SmartClassAI.Models
{
    public class ActivityLog
    {
        public int Id { get; set; }

        public string Message { get; set; }

        public string? UserId { get; set; }

        public string? Role { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}