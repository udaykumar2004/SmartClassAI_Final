using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartClassAI.DataAccess.Data;
using SmartClassAI.Models;
using SmartClassAI.Models.ViewModels;
using SmartClassAI.WebApp.Utility;

namespace SmartClassAI.WebApp.Controllers
{
    [Authorize(Roles = $"{SD.Role_Teacher},{SD.Role_Admin}")]
    public class TeacherController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<TeacherController> _logger;

        public TeacherController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<TeacherController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // =========================================================
        // ALL STUDENTS ACROSS TEACHER CLASSROOMS
        // =========================================================
        public async Task<IActionResult> Students()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                    return Unauthorized();

                bool isAdmin =
                    await _userManager.IsInRoleAsync(currentUser, SD.Role_Admin);

                // =====================================================
                // GET CLASSROOM IDS
                // =====================================================

                var classroomQuery = _context.Classrooms.AsNoTracking();

                if (!isAdmin)
                {
                    classroomQuery = classroomQuery
                        .Where(c => c.TeacherId == currentUser.Id);
                }

                var classrooms = await classroomQuery
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Subject
                    })
                    .ToListAsync();

                var classroomIds = classrooms
                    .Select(c => c.Id)
                    .ToList();

                // =====================================================
                // NO CLASSROOMS
                // =====================================================

                if (!classroomIds.Any())
                {
                    ViewBag.TotalStudents = 0;
                    ViewBag.TotalClassrooms = 0;

                    return View(new List<StudentManagementVM>());
                }

                // =====================================================
                // GET MEMBERS
                // =====================================================

                var members = await _context.ClassroomMembers
                    .AsNoTracking()
                    .Include(m => m.User)
                    .Include(m => m.Classroom)
                    .Where(m =>
                        classroomIds.Contains(m.ClassroomId) &&
                        m.Role == SD.ClassroomRole_Student &&
                        m.User != null)
                    .OrderByDescending(m => m.JoinedAt)
                    .ToListAsync();

                // =====================================================
                // OPTIMIZED SUBMISSION COUNTS
                // =====================================================

                var assignmentMap = await _context.Assignments
                    .AsNoTracking()
                    .Where(a => classroomIds.Contains(a.ClassroomId))
                    .Select(a => new
                    {
                        a.Id,
                        a.ClassroomId
                    })
                    .ToListAsync();

                var assignmentIds = assignmentMap
                    .Select(a => a.Id)
                    .ToList();

                var submissions = await _context.AssignmentSubmissions
                    .AsNoTracking()
                    .Where(s => assignmentIds.Contains(s.AssignmentId))
                    .ToListAsync();

                // =====================================================
                // BUILD VIEWMODEL
                // =====================================================

                var studentRows = members.Select(member =>
                {
                    var classroomAssignmentIds = assignmentMap
                        .Where(a => a.ClassroomId == member.ClassroomId)
                        .Select(a => a.Id)
                        .ToList();

                    int submissionCount = submissions.Count(s =>
                        s.StudentId == member.UserId &&
                        classroomAssignmentIds.Contains(s.AssignmentId));

                    return new StudentManagementVM
                    {
                        MemberId = member.Id,
                        ClassroomId = member.ClassroomId,
                        ClassroomName = member.Classroom?.Name ?? "N/A",

                        StudentId = member.UserId,

                        StudentName =
                            !string.IsNullOrWhiteSpace(member.User!.FullName)
                            ? member.User.FullName
                            : member.User.Email ?? "Unknown",

                        StudentEmail = member.User.Email ?? "N/A",

                        ProfilePicUrl = member.User.ProfilePictureUrl,

                        JoinedAt = member.JoinedAt,

                        IsActive =
                            member.User.IsActive &&
                            !member.User.IsSuspended,

                        SubmissionCount = submissionCount
                    };
                }).ToList();

                ViewBag.TotalStudents =
                    studentRows
                    .Select(s => s.StudentId)
                    .Distinct()
                    .Count();

                ViewBag.TotalClassrooms = classroomIds.Count;

                return View(studentRows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading students");

                TempData["error"] =
                    "Something went wrong while loading students.";

                return RedirectToAction("Index", "Home");
            }
        }

        // =========================================================
        // CLASSROOM STUDENTS
        // =========================================================

        public async Task<IActionResult> ClassroomStudents(int classroomId)
        {
            try
            {
                var currentUser =
                    await _userManager.GetUserAsync(User);

                if (currentUser == null)
                    return Unauthorized();

                bool isAdmin =
                    await _userManager.IsInRoleAsync(currentUser, SD.Role_Admin);

                // =====================================================
                // GET CLASSROOM
                // =====================================================

                var classroom = await _context.Classrooms
                    .AsNoTracking()
                    .Include(c => c.Teacher)
                    .FirstOrDefaultAsync(c => c.Id == classroomId);

                if (classroom == null)
                    return NotFound();

                // =====================================================
                // SECURITY CHECK
                // =====================================================

                if (!isAdmin &&
                    classroom.TeacherId != currentUser.Id)
                {
                    return Forbid();
                }

                // =====================================================
                // ASSIGNMENTS
                // =====================================================

                var assignments = await _context.Assignments
                    .AsNoTracking()
                    .Where(a => a.ClassroomId == classroomId)
                    .ToListAsync();

                var assignmentIds =
                    assignments.Select(a => a.Id).ToList();

                // =====================================================
                // MEMBERS
                // =====================================================

                var members = await _context.ClassroomMembers
                    .AsNoTracking()
                    .Include(m => m.User)
                    .Where(m =>
                        m.ClassroomId == classroomId &&
                        m.Role == SD.ClassroomRole_Student &&
                        m.User != null)
                    .OrderByDescending(m => m.JoinedAt)
                    .ToListAsync();

                // =====================================================
                // SUBMISSIONS
                // =====================================================

                var submissions = await _context.AssignmentSubmissions
                    .AsNoTracking()
                    .Where(s => assignmentIds.Contains(s.AssignmentId))
                    .ToListAsync();

                // =====================================================
                // BUILD VIEWMODEL
                // =====================================================

                var rows = members.Select(member =>
                {
                    var studentSubmissions = submissions
                        .Where(s => s.StudentId == member.UserId)
                        .ToList();

                    var gradedSubmissions = studentSubmissions
                        .Where(s =>
                            s.Status == SD.Submission_Reviewed &&
                            s.Marks.HasValue)
                        .ToList();

                    double? avgMarks = gradedSubmissions.Any()
                        ? Math.Round(
                            gradedSubmissions.Average(s => (double)s.Marks!.Value),
                            1)
                        : null;

                    double participation =
                        assignments.Any()
                        ? Math.Round(
                            (studentSubmissions.Count * 100.0)
                            / assignments.Count,
                            1)
                        : 0;

                    return new ClassroomStudentVM
                    {
                        MemberId = member.Id,

                        StudentId = member.UserId,

                        StudentName =
                            !string.IsNullOrWhiteSpace(member.User!.FullName)
                            ? member.User.FullName
                            : member.User.Email ?? "Unknown",

                        StudentEmail =
                            member.User.Email ?? "N/A",

                        ProfilePicUrl =
                            member.User.ProfilePictureUrl,

                        JoinedAt =
                            member.JoinedAt,

                        SubmissionCount =
                            studentSubmissions.Count,

                        TotalAssignments =
                            assignments.Count,

                        AverageMarks =
                            avgMarks,

                        AssignmentParticipationPct =
                            participation,

                        IsActive =
                            member.User.IsActive &&
                            !member.User.IsSuspended
                    };
                }).ToList();

                ViewBag.Classroom = classroom;
                ViewBag.AssignmentCount = assignments.Count;

                return View(rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading classroom students");

                TempData["error"] =
                    "Something went wrong while loading classroom students.";

                return RedirectToAction(nameof(Students));
            }
        }

        // =========================================================
        // REMOVE STUDENT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveStudent(
            int memberId,
            int classroomId)
        {
            try
            {
                var currentUser =
                    await _userManager.GetUserAsync(User);

                if (currentUser == null)
                    return Unauthorized();

                bool isAdmin =
                    await _userManager.IsInRoleAsync(currentUser, SD.Role_Admin);

                var classroom = await _context.Classrooms
                    .FirstOrDefaultAsync(c => c.Id == classroomId);

                if (classroom == null)
                    return NotFound();

                // =====================================================
                // SECURITY CHECK
                // =====================================================

                if (!isAdmin &&
                    classroom.TeacherId != currentUser.Id)
                {
                    return Forbid();
                }

                var member = await _context.ClassroomMembers
                    .FirstOrDefaultAsync(m => m.Id == memberId);

                if (member == null)
                {
                    TempData["error"] = "Student not found.";
                    return RedirectToAction(
                        nameof(ClassroomStudents),
                        new { classroomId });
                }

                _context.ClassroomMembers.Remove(member);

                await _context.SaveChangesAsync();

                TempData["success"] =
                    "Student removed successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing student");

                TempData["error"] =
                    "Something went wrong while removing student.";
            }

            return RedirectToAction(
                nameof(ClassroomStudents),
                new { classroomId });
        }
    }
}