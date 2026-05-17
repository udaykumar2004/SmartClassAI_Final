using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartClassAI.Models
{
    public class Material
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string FileUrl { get; set; }

        public string FileType { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.Now;

        // CLASSROOM
        public int ClassroomId { get; set; }

        [ForeignKey("ClassroomId")]
        public Classroom Classroom { get; set; }

        // UPLOADER
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }
    }
}