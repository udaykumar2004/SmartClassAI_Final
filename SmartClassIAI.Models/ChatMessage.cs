using SmartClassAI.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SmartClassAI.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }

        [Required]
        public int ClassroomId { get; set; }

        public Classroom? Classroom { get; set; }

        [Required]
        public string SenderId { get; set; } = string.Empty;

        public ApplicationUser? Sender { get; set; }

        [Required]
        public string Message { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }

}
