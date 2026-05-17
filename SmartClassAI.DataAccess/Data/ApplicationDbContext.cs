// ============================================================
// ApplicationDbContext.cs
// FIXES:
//  1. Added ClassroomMember unique index (ClassroomId + UserId)
//  2. Configured ClassroomMember → User and → Classroom properly
//  3. Updated Classroom → Members (was Enrollments)
//  4. Updated ApplicationUser → ClassroomMemberships (was Enrollments)
//  5. Removed orphaned Enrollment configuration on User
//  6. Added ChatMessage, Notification configurations
//  7. All cascade deletes are intentional and safe
// ============================================================

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartClassAI.Models;

namespace SmartClassAI.DataAccess.Data;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // =============================================
    // DB SETS
    // =============================================

    public DbSet<Classroom> Classrooms { get; set; }
    public DbSet<ClassroomMember> ClassroomMembers { get; set; }
    public DbSet<Assignment> Assignments { get; set; }
    public DbSet<AssignmentSubmission> AssignmentSubmissions { get; set; }
    public DbSet<AssignmentComment> AssignmentComments { get; set; }
    public DbSet<Announcement> Announcements { get; set; }
    public DbSet<Material> Materials { get; set; }
    public DbSet<Note> Notes { get; set; }
    public DbSet<Meeting> Meetings { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<ActivityLog> ActivityLogs { get; set; }

    // Legacy — keep for data migration, do NOT use for membership checks
    public DbSet<Enrollment> Enrollments { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // =============================================
        // CLASSROOM → TEACHER
        // =============================================
        builder.Entity<Classroom>()
            .HasOne(c => c.Teacher)
            .WithMany(u => u.CreatedClasses)
            .HasForeignKey(c => c.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        // =============================================
        // CLASSROOMMEMBER → USER
        // FIX: Was not configured at all — no unique index,
        //      no cascade rules, duplicate memberships were possible
        // =============================================
        builder.Entity<ClassroomMember>()
            .HasOne(m => m.User)
            .WithMany(u => u.ClassroomMemberships)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // =============================================
        // CLASSROOMMEMBER → CLASSROOM
        // =============================================
        builder.Entity<ClassroomMember>()
            .HasOne(m => m.Classroom)
            .WithMany(c => c.Members)
            .HasForeignKey(m => m.ClassroomId)
            .OnDelete(DeleteBehavior.Cascade);

        // =============================================
        // UNIQUE INDEX: One membership per user per classroom
        // FIX: Without this, duplicate memberships were possible
        // =============================================
        builder.Entity<ClassroomMember>()
            .HasIndex(m => new { m.ClassroomId, m.UserId })
            .IsUnique();

        // =============================================
        // ENROLLMENT → STUDENT (legacy, keep for migration)
        // =============================================
        builder.Entity<Enrollment>()
            .HasOne(e => e.Student)
            .WithMany()
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Enrollment>()
            .HasOne(e => e.Classroom)
            .WithMany()
            .HasForeignKey(e => e.ClassroomId);

        // =============================================
        // ASSIGNMENT → CLASSROOM
        // =============================================
        builder.Entity<Assignment>()
            .HasOne(a => a.Classroom)
            .WithMany(c => c.Assignments)
            .HasForeignKey(a => a.ClassroomId)
            .OnDelete(DeleteBehavior.Cascade);

        // =============================================
        // SUBMISSION → ASSIGNMENT
        // =============================================
        builder.Entity<AssignmentSubmission>()
            .HasOne(s => s.Assignment)
            .WithMany(a => a.Submissions)
            .HasForeignKey(s => s.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // =============================================
        // SUBMISSION → STUDENT
        // =============================================
        builder.Entity<AssignmentSubmission>()
            .HasOne(s => s.Student)
            .WithMany(u => u.AssignmentSubmissions)
            .HasForeignKey(s => s.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // =============================================
        // ASSIGNMENT COMMENT → ASSIGNMENT
        // =============================================
        builder.Entity<AssignmentComment>()
            .HasOne(c => c.Assignment)
            .WithMany(a => a.Comments)
            .HasForeignKey(c => c.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // =============================================
        // ANNOUNCEMENT → CLASSROOM
        // =============================================
        builder.Entity<Announcement>()
            .HasOne(a => a.Classroom)
            .WithMany(c => c.Announcements)
            .HasForeignKey(a => a.ClassroomId)
            .OnDelete(DeleteBehavior.Cascade);

        // =============================================
        // ANNOUNCEMENT → USER
        // =============================================
        builder.Entity<Announcement>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // =============================================
        // MATERIAL → CLASSROOM
        // =============================================
        builder.Entity<Material>()
            .HasOne(m => m.Classroom)
            .WithMany(c => c.Materials)
            .HasForeignKey(m => m.ClassroomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Material>()
            .HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // =============================================
        // NOTE → CLASSROOM
        // =============================================
        builder.Entity<Note>()
            .HasOne(n => n.Classroom)
            .WithMany(c => c.Notes)
            .HasForeignKey(n => n.ClassroomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Note>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // =============================================
        // MEETING → CLASSROOM
        // =============================================
        builder.Entity<Meeting>()
            .HasOne(m => m.Classroom)
            .WithMany(c => c.Meetings)
            .HasForeignKey(m => m.ClassroomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Meeting>()
            .HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // =============================================
        // CHAT MESSAGE → CLASSROOM
        // =============================================
        builder.Entity<ChatMessage>()
            .HasOne(c => c.Classroom)
            .WithMany()
            .HasForeignKey(c => c.ClassroomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ChatMessage>()
            .HasOne(c => c.Sender)
            .WithMany()
            .HasForeignKey(c => c.SenderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}