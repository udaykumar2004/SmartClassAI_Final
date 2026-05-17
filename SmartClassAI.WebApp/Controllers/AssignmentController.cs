using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartClassAI.DataAccess.Data;
using SmartClassAI.DataAccess.Repository.IRepository;
using SmartClassAI.Models;
using SmartClassAI.WebApp.Services;
using SmartClassAI.WebApp.Utility;

namespace SmartClassAI.WebApp.Controllers
{
    [Authorize]
    public class AssignmentController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly EmailTemplateService _email;

        public AssignmentController(
            IUnitOfWork unitOfWork,
            IWebHostEnvironment webHostEnvironment,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            EmailTemplateService email)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
            _context = context;
            _email = email;
        }

        // =========================================================
        // INDEX
        // =========================================================
        public async Task<IActionResult> Index(int classroomId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var classroom = await _context.Classrooms
                .AsNoTracking()
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(c => c.Id == classroomId);

            if (classroom == null)
            {
                TempData["error"] = "Classroom not found.";
                return RedirectToAction("Index", "Classroom");
            }

            bool isAdmin =
                await _userManager.IsInRoleAsync(user, SD.Role_Admin);

            bool isTeacher =
                classroom.TeacherId == user.Id;

            bool isMember =
                await _context.ClassroomMembers
                    .AnyAsync(x =>
                        x.ClassroomId == classroomId &&
                        x.UserId == user.Id);

            if (!isAdmin && !isTeacher && !isMember)
                return Forbid();

            var assignments = await _context.Assignments
                .AsNoTracking()
                .Include(a => a.Classroom)
                .Where(a => a.ClassroomId == classroomId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            var assignmentIds = assignments
                .Select(a => a.Id)
                .ToList();

            var submissionCounts = await _context.AssignmentSubmissions
                .AsNoTracking()
                .Where(s => assignmentIds.Contains(s.AssignmentId))
                .GroupBy(s => s.AssignmentId)
                .Select(g => new
                {
                    AssignmentId = g.Key,
                    Count = g.Count()
                })
                .ToDictionaryAsync(x => x.AssignmentId, x => x.Count);

            Dictionary<int, string> mySubmissions = new();

            if (!isTeacher && !isAdmin)
            {
                var subs = await _context.AssignmentSubmissions
                    .AsNoTracking()
                    .Where(s =>
                        s.StudentId == user.Id &&
                        assignmentIds.Contains(s.AssignmentId))
                    .ToListAsync();

                mySubmissions = subs
                    .GroupBy(s => s.AssignmentId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(x => x.SubmittedAt)
                              .First()
                              .Status
                    );
            }

            int studentCount = await _context.ClassroomMembers
                .AsNoTracking()
                .CountAsync(m =>
                    m.ClassroomId == classroomId &&
                    m.Role == SD.ClassroomRole_Student);

            ViewBag.ClassroomId = classroomId;
            ViewBag.ClassroomName = classroom.Name;
            ViewBag.TeacherName =
                classroom.Teacher?.FullName ??
                classroom.Teacher?.Email ??
                "Teacher";

            ViewBag.IsClassTeacher = isTeacher;
            ViewBag.IsAdmin = isAdmin;
            ViewBag.SubmissionCounts = submissionCounts;
            ViewBag.MySubmissions = mySubmissions;
            ViewBag.StudentCount = studentCount;

            return View(assignments);
        }

        // =========================================================
        // DETAILS
        // =========================================================
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var assignment = await _context.Assignments
                .Include(a => a.Classroom)
                .Include(a => a.Comments)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assignment?.Classroom == null)
                return NotFound();

            bool isAdmin =
                await _userManager.IsInRoleAsync(user, SD.Role_Admin);

            bool isTeacher =
                assignment.Classroom.TeacherId == user.Id;

            bool isMember =
                await _context.ClassroomMembers
                    .AnyAsync(x =>
                        x.ClassroomId == assignment.ClassroomId &&
                        x.UserId == user.Id);

            if (!isAdmin && !isTeacher && !isMember)
                return Forbid();

            AssignmentSubmission? mySubmission = null;

            if (!isTeacher && !isAdmin)
            {
                mySubmission = await _context.AssignmentSubmissions
                    .AsNoTracking()
                    .OrderByDescending(s => s.SubmittedAt)
                    .FirstOrDefaultAsync(s =>
                        s.AssignmentId == id &&
                        s.StudentId == user.Id);
            }

            int submissionCount = await _context.AssignmentSubmissions
                .AsNoTracking()
                .CountAsync(s => s.AssignmentId == id);

            // Optional View Count
            assignment.ViewCount++;
            await _context.SaveChangesAsync();

            ViewBag.IsClassTeacher = isTeacher;
            ViewBag.IsAdmin = isAdmin;
            ViewBag.MySubmission = mySubmission;
            ViewBag.SubmissionCount = submissionCount;
            ViewBag.IsOverdue =
                assignment.DueDate < DateTime.UtcNow;

            return View(assignment);
        }

        // =========================================================
        // UPSERT GET
        // =========================================================
        public async Task<IActionResult> Upsert(
            int? id,
            int classroomId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var classroom = await _context.Classrooms
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == classroomId);

            if (classroom == null)
            {
                TempData["error"] = "Classroom not found.";
                return RedirectToAction("Index", "Classroom");
            }

            bool isAdmin =
                await _userManager.IsInRoleAsync(user, SD.Role_Admin);

            bool isTeacher =
                classroom.TeacherId == user.Id;

            if (!isAdmin && !isTeacher)
                return Forbid();

            if (id == null || id == 0)
            {
                return View(new Assignment
                {
                    ClassroomId = classroomId,
                    DueDate = DateTime.UtcNow.AddDays(7),
                    TotalMarks = 100,
                    Status = SD.Assignment_Published
                });
            }

            var assignment = await _context.Assignments
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (assignment == null)
                return NotFound();

            if (!isAdmin && classroom.TeacherId != user.Id)
                return Forbid();

            return View(assignment);
        }

        // =========================================================
        // UPSERT POST
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(
            Assignment obj,
            IFormFile? file)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var classroom = await _context.Classrooms
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(c => c.Id == obj.ClassroomId);

            if (classroom == null)
            {
                TempData["error"] = "Invalid classroom.";
                return RedirectToAction("Index", "Classroom");
            }

            bool isAdmin =
                await _userManager.IsInRoleAsync(user, SD.Role_Admin);

            bool isTeacher =
                classroom.TeacherId == user.Id;

            if (!isAdmin && !isTeacher)
                return Forbid();

            // ================= VALIDATION =================

            if (string.IsNullOrWhiteSpace(obj.Title))
                ModelState.AddModelError(
                    "Title",
                    "Title is required.");

            if (string.IsNullOrWhiteSpace(obj.Description))
                ModelState.AddModelError(
                    "Description",
                    "Description is required.");

            if (obj.TotalMarks <= 0)
                ModelState.AddModelError(
                    "TotalMarks",
                    "Marks must be greater than 0.");

            if (obj.DueDate < DateTime.UtcNow.AddMinutes(-5))
                ModelState.AddModelError(
                    "DueDate",
                    "Due date cannot be in the past.");

            Assignment? existingDb = null;

            if (obj.Id != 0)
            {
                existingDb = await _context.Assignments
                    .FirstOrDefaultAsync(x => x.Id == obj.Id);

                if (existingDb == null)
                    return NotFound();

                var existingClassroom = await _context.Classrooms
                    .FirstOrDefaultAsync(c =>
                        c.Id == existingDb.ClassroomId);

                if (!isAdmin &&
                    existingClassroom?.TeacherId != user.Id)
                {
                    return Forbid();
                }

                obj.FileUrl = existingDb.FileUrl;
                obj.CreatedAt = existingDb.CreatedAt;
                obj.ViewCount = existingDb.ViewCount;
            }
            else
            {
                obj.CreatedAt = DateTime.UtcNow;
            }

            ModelState.Remove("Classroom");

            if (!ModelState.IsValid)
                return View(obj);

            // ================= FILE UPLOAD =================

            if (file != null)
            {
                string ext =
                    Path.GetExtension(file.FileName).ToLower();

                if (!SD.Allowed_Assignment_Extensions.Contains(ext))
                {
                    TempData["error"] =
                        $"File type '{ext}' is not allowed.";

                    return View(obj);
                }

                if (file.Length > SD.MaxFileSize_Bytes)
                {
                    TempData["error"] =
                        "File size exceeds 10 MB limit.";

                    return View(obj);
                }

                string uploadDir = Path.Combine(
                    _webHostEnvironment.WebRootPath,
                    SD.Upload_Folder);

                Directory.CreateDirectory(uploadDir);

                // Delete old file
                if (!string.IsNullOrWhiteSpace(existingDb?.FileUrl))
                {
                    string oldPath = Path.Combine(
                        _webHostEnvironment.WebRootPath,
                        existingDb.FileUrl.TrimStart('/'));

                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                string fileName =
                    $"{Guid.NewGuid()}{ext}";

                string fullPath =
                    Path.Combine(uploadDir, fileName);

                using (var stream = new FileStream(
                    fullPath,
                    FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                obj.FileUrl =
                    $"/{SD.Upload_Folder}/{fileName}";
            }

            bool isCreate = obj.Id == 0;

            if (isCreate)
            {
                _context.Assignments.Add(obj);
                await _context.SaveChangesAsync();

                TempData["success"] =
                    "Assignment created successfully!";

                // ================= EMAIL =================

                try
                {
                    var students = await _context.ClassroomMembers
                        .Include(m => m.User)
                        .Where(m =>
                            m.ClassroomId == obj.ClassroomId &&
                            m.Role == SD.ClassroomRole_Student &&
                            m.User != null)
                        .ToListAsync();

                    foreach (var s in students)
                    {
                        if (string.IsNullOrWhiteSpace(s.User?.Email))
                            continue;

                        string studentName =
                            !string.IsNullOrWhiteSpace(s.User.FullName)
                            ? s.User.FullName
                            : s.User.Email;

                        await _email.SendAssignmentCreatedAsync(
                            s.User.Email,
                            studentName,
                            classroom.Teacher?.FullName ??
                            "Your Teacher",
                            classroom.Name,
                            obj.Title,
                            obj.Description.Length > 150
                                ? obj.Description[..150] + "..."
                                : obj.Description,
                            obj.DueDate,
                            obj.TotalMarks);
                    }
                }
                catch
                {
                    // Optional logging
                }
            }
            else
            {
                _context.Assignments.Update(obj);
                await _context.SaveChangesAsync();

                TempData["success"] =
                    "Assignment updated successfully!";
            }

            return RedirectToAction(
                nameof(Index),
                new { classroomId = obj.ClassroomId });
        }

        // =========================================================
        // DELETE
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var assignment = await _context.Assignments
                .Include(a => a.Classroom)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (assignment == null)
                return NotFound();

            bool isAdmin =
                await _userManager.IsInRoleAsync(user, SD.Role_Admin);

            bool isTeacher =
                assignment.Classroom?.TeacherId == user.Id;

            if (!isAdmin && !isTeacher)
                return Forbid();

            int classroomId = assignment.ClassroomId;

            // Delete assignment file
            if (!string.IsNullOrWhiteSpace(assignment.FileUrl))
            {
                string path = Path.Combine(
                    _webHostEnvironment.WebRootPath,
                    assignment.FileUrl.TrimStart('/'));

                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }

            // Delete submission files
            var submissions = await _context.AssignmentSubmissions
                .Where(x => x.AssignmentId == id)
                .ToListAsync();

            foreach (var submission in submissions)
            {
                if (!string.IsNullOrWhiteSpace(
                    submission.SubmissionFileUrl))
                {
                    string subPath = Path.Combine(
                        _webHostEnvironment.WebRootPath,
                        submission.SubmissionFileUrl.TrimStart('/'));

                    if (System.IO.File.Exists(subPath))
                        System.IO.File.Delete(subPath);
                }

                _context.AssignmentSubmissions.Remove(submission);
            }

            _context.Assignments.Remove(assignment);

            await _context.SaveChangesAsync();

            TempData["success"] =
                "Assignment deleted successfully.";

            return RedirectToAction(
                nameof(Index),
                new { classroomId });
        }
    }
}