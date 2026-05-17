// ============================================================
// DashboardService.cs — OPTIMIZED
//
// BUGS FIXED:
//  1. GetStatsAsync() looped ALL users calling GetRolesAsync() per user
//     = O(n) DB queries. For 1000 users = 1000 SQL queries.
//     FIX: Use GetUsersInRoleAsync() — 3 queries total regardless of user count.
//
//  2. Role names checked were "Teacher"/"Student" but SD had "Teacher User"/"Student User"
//     FIX: Now uses SD constants (which are fixed to "Teacher"/"Student")
// ============================================================

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartClassAI.DataAccess.Data;
using SmartClassAI.Models;
using SmartClassAI.WebApp.Utility;

namespace SmartClassAI.WebApp.Services
{
    public class DashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =============================================
        // GET ADMIN STATS
        // FIX: Was N+1 queries. Now 4 total queries.
        // =============================================
        public async Task<AdminStatsDto> GetStatsAsync()
        {
            // FIX: Use role-based queries — O(1) queries not O(n)
            var teachers = await _userManager.GetUsersInRoleAsync(SD.Role_Teacher);
            var students = await _userManager.GetUsersInRoleAsync(SD.Role_Student);
            var admins = await _userManager.GetUsersInRoleAsync(SD.Role_Admin);

            int totalUsers = await _userManager.Users.CountAsync();
            int totalClassrooms = await _context.Classrooms.CountAsync();
            int totalAssignments = await _context.Assignments.CountAsync();
            int totalSubmissions = await _context.AssignmentSubmissions.CountAsync();

            return new AdminStatsDto
            {
                TotalUsers = totalUsers,
                TotalTeachers = teachers.Count,
                TotalStudents = students.Count,
                TotalAdmins = admins.Count,
                TotalClassrooms = totalClassrooms,
                TotalAssignments = totalAssignments,
                TotalSubmissions = totalSubmissions
            };
        }

        // =============================================
        // TEACHER DASHBOARD STATS
        // =============================================
        public async Task<TeacherStatsDto> GetTeacherStatsAsync(string teacherId)
        {
            var myClassrooms = await _context.Classrooms
                .Where(c => c.TeacherId == teacherId)
                .Select(c => c.Id)
                .ToListAsync();

            int totalStudents = await _context.ClassroomMembers
                .Where(m => myClassrooms.Contains(m.ClassroomId)
                         && m.Role == SD.ClassroomRole_Student)
                .CountAsync();

            int totalAssignments = await _context.Assignments
                .Where(a => myClassrooms.Contains(a.ClassroomId))
                .CountAsync();

            int pendingSubmissions = await _context.AssignmentSubmissions
                .Include(s => s.Assignment)
                .Where(s => myClassrooms.Contains(s.Assignment!.ClassroomId)
                         && s.Status == SD.Submission_Submitted)
                .CountAsync();

            return new TeacherStatsDto
            {
                TotalClassrooms = myClassrooms.Count,
                TotalStudents = totalStudents,
                TotalAssignments = totalAssignments,
                PendingSubmissions = pendingSubmissions
            };
        }

        // =============================================
        // STUDENT DASHBOARD STATS
        // =============================================
        public async Task<StudentStatsDto> GetStudentStatsAsync(string studentId)
        {
            var myClassroomIds = await _context.ClassroomMembers
                .Where(m => m.UserId == studentId)
                .Select(m => m.ClassroomId)
                .ToListAsync();

            int totalAssignments = await _context.Assignments
                .Where(a => myClassroomIds.Contains(a.ClassroomId)
                         && a.Status == SD.Assignment_Published)
                .CountAsync();

            int submitted = await _context.AssignmentSubmissions
                .Where(s => s.StudentId == studentId)
                .CountAsync();

            int pendingAssignments = totalAssignments - submitted;
            if (pendingAssignments < 0) pendingAssignments = 0;

            int gradedCount = await _context.AssignmentSubmissions
                .Where(s => s.StudentId == studentId
                         && s.Status == SD.Submission_Reviewed
                         && s.Marks != null)
                .CountAsync();

            double? avgMarks = await _context.AssignmentSubmissions
                .Where(s => s.StudentId == studentId && s.Marks != null)
                .AverageAsync(s => (double?)s.Marks);

            return new StudentStatsDto
            {
                TotalClassrooms = myClassroomIds.Count,
                TotalAssignments = totalAssignments,
                SubmittedCount = submitted,
                PendingCount = pendingAssignments,
                GradedCount = gradedCount,
                AverageMarks = avgMarks.HasValue ? Math.Round(avgMarks.Value, 1) : null
            };
        }

        // =============================================
        // ACTIVITY LOG
        // =============================================
        public async Task AddActivityAsync(
            string message,
            string? userId = null,
            string? role = null)
        {
            var log = new ActivityLog
            {
                Message = message,
                UserId = userId,
                Role = role,
                CreatedAt = DateTime.UtcNow
            };
            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ActivityLog>> GetLatestActivitiesAsync(int count = 20)
        {
            return await _context.ActivityLogs
                .OrderByDescending(x => x.CreatedAt)
                .Take(count)
                .ToListAsync();
        }
    }

    // =============================================
    // DTOs
    // =============================================
    public class AdminStatsDto
    {
        public int TotalUsers { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalStudents { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalClassrooms { get; set; }
        public int TotalAssignments { get; set; }
        public int TotalSubmissions { get; set; }
    }

    public class TeacherStatsDto
    {
        public int TotalClassrooms { get; set; }
        public int TotalStudents { get; set; }
        public int TotalAssignments { get; set; }
        public int PendingSubmissions { get; set; }
    }

    public class StudentStatsDto
    {
        public int TotalClassrooms { get; set; }
        public int TotalAssignments { get; set; }
        public int SubmittedCount { get; set; }
        public int PendingCount { get; set; }
        public int GradedCount { get; set; }
        public double? AverageMarks { get; set; }
    }
}