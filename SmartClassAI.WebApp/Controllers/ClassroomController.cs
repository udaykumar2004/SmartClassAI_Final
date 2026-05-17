using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartClassAI.DataAccess.Data;
using SmartClassAI.DataAccess.Repository.IRepository;
using SmartClassAI.Models;
using SmartClassAI.Models.ViewModels;
using SmartClassAI.WebApp.Services;
using SmartClassAI.WebApp.Utility;
using System.Security.Claims;

namespace SmartClassAI.WebApp.Controllers
{
    [Authorize]
    public class ClassroomController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly EmailTemplateService _email;

        public ClassroomController(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            EmailTemplateService email)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _context = context;
            _email = email;
        }

        // =========================================================
        // INDEX
        // =========================================================
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            bool isAdmin = await _userManager.IsInRoleAsync(user, SD.Role_Admin);

            if (isAdmin)
            {
                var allClasses = await _unitOfWork.Classroom.GetAllAsync(
                    includeProperties: "Teacher");
                return View(allClasses);
            }

            var teacherClassIds = (await _unitOfWork.Classroom.GetAllAsync(
                    x => x.TeacherId == user.Id))
                .Select(x => x.Id).ToHashSet();

            var memberClassIds = (await _unitOfWork.ClassroomMember.GetAllAsync(
                    x => x.UserId == user.Id))
                .Select(x => x.ClassroomId).ToHashSet();

            var allClassIds = teacherClassIds.Union(memberClassIds).ToList();

            if (!allClassIds.Any())
                return View(new List<Classroom>());

            // Load classrooms with teacher + member counts
            var classrooms = await _context.Classrooms
                .Include(c => c.Teacher)
                .Include(c => c.Members)
                .Where(c => allClassIds.Contains(c.Id))
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            ViewBag.CurrentUserId = user.Id;
            ViewBag.MemberClassIds = memberClassIds;
            ViewBag.TeacherClassIds = teacherClassIds;

            return View(classrooms);
        }

        // =========================================================
        // DETAILS
        // =========================================================
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var classroom = await _unitOfWork.Classroom.GetAsync(
                x => x.Id == id,
                includeProperties:
                    "Teacher,Members.User,Assignments,Announcements.User,Notes.User,Materials.User,Meetings.User");

            if (classroom == null) return NotFound();

            bool isAdmin = await _userManager.IsInRoleAsync(user, SD.Role_Admin);
            bool isTeacher = classroom.TeacherId == user.Id;
            bool isMember = classroom.Members.Any(m => m.UserId == user.Id);

            if (!isAdmin && !isTeacher && !isMember)
                return Forbid();

            var vm = BuildViewModel(classroom, user.Id, isAdmin, isTeacher);

            ViewBag.IsAdmin = isAdmin;
            ViewBag.IsClassTeacher = isTeacher;
            ViewBag.IsMember = isMember;
            ViewBag.CurrentUserId = user.Id;

            return View(vm);
        }

        // =========================================================
        // UPSERT GET
        // =========================================================
        public async Task<IActionResult> Upsert(int? id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (id == null || id == 0)
                return View(new Classroom());

            var classroom = await _unitOfWork.Classroom.GetAsync(x => x.Id == id);
            if (classroom == null) return NotFound();

            bool isAdmin = await _userManager.IsInRoleAsync(user, SD.Role_Admin);
            if (!isAdmin && classroom.TeacherId != user.Id) return Forbid();

            return View(classroom);
        }

        // =========================================================
        // UPSERT POST  ← Email sent on create
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(Classroom obj)
        {
            // Remove nav property validation noise
            ModelState.Remove("Teacher");
            ModelState.Remove("Members");
            ModelState.Remove("Assignments");
            ModelState.Remove("Announcements");
            ModelState.Remove("Materials");
            ModelState.Remove("Notes");
            ModelState.Remove("Meetings");

            if (!ModelState.IsValid)
                return View(obj);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (obj.Id == 0)
            {
                // CREATE
                bool isTeacher = await _userManager.IsInRoleAsync(user, SD.Role_Teacher);
                bool isAdmin = await _userManager.IsInRoleAsync(user, SD.Role_Admin);
                if (!isTeacher && !isAdmin)
                    await _userManager.AddToRoleAsync(user, SD.Role_Teacher);

                obj.TeacherId = user.Id;
                obj.CreatedAt = DateTime.UtcNow;
                obj.JoinCode = Guid.NewGuid().ToString()[..6].ToUpper();

                _unitOfWork.Classroom.Add(obj);
                await _unitOfWork.SaveAsync();

                // Add teacher as classroom member
                _unitOfWork.ClassroomMember.Add(new ClassroomMember
                {
                    ClassroomId = obj.Id,
                    UserId = user.Id,
                    Role = SD.ClassroomRole_Teacher,
                    JoinedAt = DateTime.UtcNow
                });
                await _unitOfWork.SaveAsync();

                TempData["success"] = "Classroom created!";

                // Send confirmation email to teacher (fire and forget)
                if (!string.IsNullOrEmpty(user.Email))
                {
                    _ = _email.SendClassroomCreatedAsync(
                        user.Email,
                        user.FullName.Length > 0 ? user.FullName : user.Email,
                        obj.Name,
                        obj.Subject,
                        obj.JoinCode);
                }
            }
            else
            {
                // UPDATE
                var existing = await _unitOfWork.Classroom.GetAsync(x => x.Id == obj.Id);
                if (existing == null) return NotFound();

                bool isAdmin = await _userManager.IsInRoleAsync(user, SD.Role_Admin);
                if (!isAdmin && existing.TeacherId != user.Id) return Forbid();

                obj.TeacherId = existing.TeacherId;
                obj.JoinCode = existing.JoinCode;
                obj.CreatedAt = existing.CreatedAt;

                _unitOfWork.Classroom.Update(obj);
                await _unitOfWork.SaveAsync();

                TempData["success"] = "Classroom updated!";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // DELETE
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var classroom = await _unitOfWork.Classroom.GetAsync(x => x.Id == id);
            if (classroom == null) return NotFound();

            bool isAdmin = await _userManager.IsInRoleAsync(user, SD.Role_Admin);
            if (!isAdmin && classroom.TeacherId != user.Id) return Forbid();

            _unitOfWork.Classroom.Remove(classroom);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Classroom deleted.";
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // REGENERATE CODE
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegenerateCode(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var classroom = await _unitOfWork.Classroom.GetAsync(x => x.Id == id);
            if (classroom == null) return NotFound();

            bool isAdmin = await _userManager.IsInRoleAsync(user, SD.Role_Admin);
            if (!isAdmin && classroom.TeacherId != user.Id) return Forbid();

            classroom.JoinCode = Guid.NewGuid().ToString()[..6].ToUpper();
            _unitOfWork.Classroom.Update(classroom);
            await _unitOfWork.SaveAsync();

            TempData["success"] = $"New join code: {classroom.JoinCode}";
            return RedirectToAction(nameof(Details), new { id });
        }

        // =========================================================
        // JOIN GET
        // =========================================================
        public IActionResult Join()
        {
            // Pre-fill code if redirected from legacy enrollment
            if (TempData["JoinCode"] is string code)
                ViewBag.PrefilledCode = code;
            return View();
        }

        // =========================================================
        // JOIN POST  ← Email sent to teacher on student join
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(string joinCode)
        {
            if (string.IsNullOrWhiteSpace(joinCode))
            {
                TempData["error"] = "Join code is required.";
                return View();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            joinCode = joinCode.Trim().ToUpper();

            var classroom = await _context.Classrooms
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(c => c.JoinCode == joinCode);

            if (classroom == null)
            {
                TempData["error"] = "Invalid join code.";
                return View();
            }

            if (classroom.IsArchived)
            {
                TempData["error"] = "This classroom has been archived.";
                return View();
            }

            if (classroom.TeacherId == user.Id)
            {
                TempData["error"] = "You are already the teacher of this classroom.";
                return RedirectToAction(nameof(Details), new { id = classroom.Id });
            }

            bool alreadyMember = await _unitOfWork.ClassroomMember.AnyAsync(
                x => x.ClassroomId == classroom.Id && x.UserId == user.Id);

            if (alreadyMember)
            {
                TempData["error"] = "You have already joined this classroom.";
                return RedirectToAction(nameof(Details), new { id = classroom.Id });
            }

            bool isStudent = await _userManager.IsInRoleAsync(user, SD.Role_Student);
            if (!isStudent)
                await _userManager.AddToRoleAsync(user, SD.Role_Student);

            _unitOfWork.ClassroomMember.Add(new ClassroomMember
            {
                ClassroomId = classroom.Id,
                UserId = user.Id,
                Role = SD.ClassroomRole_Student,
                JoinedAt = DateTime.UtcNow
            });
            await _unitOfWork.SaveAsync();

            TempData["success"] = $"Welcome to {classroom.Name}!";

            // Notify teacher (fire and forget)
            if (!string.IsNullOrEmpty(classroom.Teacher?.Email))
            {
                _ = _email.SendStudentJoinedAsync(
                    classroom.Teacher.Email,
                    classroom.Teacher.FullName.Length > 0
                        ? classroom.Teacher.FullName
                        : classroom.Teacher.Email,
                    user.FullName.Length > 0 ? user.FullName : user.Email!,
                    user.Email ?? "",
                    classroom.Name);
            }

            return RedirectToAction(nameof(Details), new { id = classroom.Id });
        }

        // =========================================================
        // LEAVE
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Leave(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var classroom = await _unitOfWork.Classroom.GetAsync(x => x.Id == id);
            if (classroom == null) return NotFound();

            if (classroom.TeacherId == user.Id)
            {
                TempData["error"] = "Teachers cannot leave their own classroom.";
                return RedirectToAction(nameof(Index));
            }

            var member = await _unitOfWork.ClassroomMember.GetAsync(
                x => x.ClassroomId == id && x.UserId == user.Id);

            if (member == null)
            {
                TempData["error"] = "You are not in this classroom.";
                return RedirectToAction(nameof(Index));
            }

            _unitOfWork.ClassroomMember.Remove(member);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "You left the classroom.";
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // REMOVE STUDENT
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveStudent(int classroomId, string studentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var classroom = await _unitOfWork.Classroom.GetAsync(x => x.Id == classroomId);
            if (classroom == null) return NotFound();

            bool isAdmin = await _userManager.IsInRoleAsync(user, SD.Role_Admin);
            bool isTeacher = classroom.TeacherId == user.Id;
            if (!isAdmin && !isTeacher) return Forbid();

            var member = await _unitOfWork.ClassroomMember.GetAsync(
                x => x.ClassroomId == classroomId && x.UserId == studentId);

            if (member == null)
            {
                TempData["error"] = "Student not found in this classroom.";
                return RedirectToAction(nameof(Details), new { id = classroomId });
            }

            _unitOfWork.ClassroomMember.Remove(member);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Student removed from classroom.";
            return RedirectToAction(nameof(Details), new { id = classroomId });
        }

        // =========================================================
        // TOGGLE ARCHIVE
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleArchive(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var classroom = await _unitOfWork.Classroom.GetAsync(x => x.Id == id);
            if (classroom == null) return NotFound();

            bool isAdmin = await _userManager.IsInRoleAsync(user, SD.Role_Admin);
            if (!isAdmin && classroom.TeacherId != user.Id) return Forbid();

            classroom.IsArchived = !classroom.IsArchived;
            classroom.ArchivedAt = classroom.IsArchived ? DateTime.UtcNow : null;

            _unitOfWork.Classroom.Update(classroom);
            await _unitOfWork.SaveAsync();

            string state = classroom.IsArchived ? "archived" : "restored";
            TempData["success"] = $"Classroom {state}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // =========================================================
        // BUILD VIEW MODEL
        // =========================================================
        private ClassroomDetailsVM BuildViewModel(
            Classroom classroom,
            string currentUserId,
            bool isAdmin,
            bool isTeacher)
        {
            var studentMembers = classroom.Members
                .Where(m => m.Role == SD.ClassroomRole_Student && m.User != null)
                .ToList();

            return new ClassroomDetailsVM
            {
                Classroom = classroom,
                StudentCount = studentMembers.Count,
                AssignmentCount = classroom.Assignments?.Count ?? 0,
                AnnouncementCount = classroom.Announcements?.Count ?? 0,
                NotesCount = classroom.Notes?.Count ?? 0,
                MaterialsCount = classroom.Materials?.Count ?? 0,
                MeetingsCount = classroom.Meetings?.Count ?? 0,
                IsLive = classroom.IsLive,
                IsClassTeacher = isTeacher,
                IsAdmin = isAdmin,
                IsEnrolledStudent = !isTeacher &&
                                    classroom.Members.Any(m => m.UserId == currentUserId),
                Students = studentMembers.Select(m => m.User!).ToList(),
                Assignments = classroom.Assignments?.OrderByDescending(x => x.CreatedAt).ToList() ?? new(),
                Announcements = classroom.Announcements?.OrderByDescending(x => x.CreatedAt).ToList() ?? new(),
                Notes = classroom.Notes?.OrderByDescending(x => x.UploadedAt).ToList() ?? new(),
                Materials = classroom.Materials?.OrderByDescending(x => x.UploadedAt).ToList() ?? new(),
                Meetings = classroom.Meetings?.OrderByDescending(x => x.StartedAt).ToList() ?? new()
            };
        }
        [HttpPost]
        [Authorize]
        public IActionResult DeleteStudent(string userId, int classroomId)
        {
            var currentUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Verify current user is teacher of this class
            var teacherMember = _unitOfWork.ClassroomMember
                .Get(x =>
                    x.ClassroomId == classroomId &&
                    x.UserId == currentUserId &&
                    x.Role == "Teacher");

            if (teacherMember == null &&
                !User.IsInRole(SD.Role_Admin))
            {
                return Forbid();
            }

            // Find student
            var studentMember = _unitOfWork.ClassroomMember
                .Get(x =>
                    x.ClassroomId == classroomId &&
                    x.UserId == userId &&
                    x.Role == "Student");

            if (studentMember == null)
            {
                TempData["error"] = "Student not found";
                return RedirectToAction(nameof(Details),
                    new { id = classroomId });
            }

            _unitOfWork.ClassroomMember.Remove(studentMember);

            _unitOfWork.Save();

            TempData["success"] =
                "Student deleted from classroom";

            return RedirectToAction(nameof(Details),
                new { id = classroomId });
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ToggleBlockUser(
    string userId,
    int classroomId)
        {
            var currentUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Verify teacher of classroom
            var teacherMember = _unitOfWork.ClassroomMember
                .Get(x =>
                    x.ClassroomId == classroomId &&
                    x.UserId == currentUserId &&
                    x.Role == "Teacher");

            if (teacherMember == null &&
                !User.IsInRole(SD.Role_Admin))
            {
                return Forbid();
            }

            var user =
                await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound();

            // Prevent blocking admin
            if (await _userManager.IsInRoleAsync(user, SD.Role_Admin))
            {
                TempData["error"] =
                    "Admin cannot be blocked";

                return RedirectToAction(nameof(Details),
                    new { id = classroomId });
            }

            // TOGGLE LOCK
            if (user.LockoutEnd != null &&
                user.LockoutEnd > DateTimeOffset.Now)
            {
                // UNLOCK
                user.LockoutEnd = null;

                TempData["success"] =
                    "User unlocked successfully";
            }
            else
            {
                // BLOCK
                user.LockoutEnd = DateTimeOffset.MaxValue;

                TempData["success"] =
                    "User blocked successfully";
            }

            await _userManager.UpdateAsync(user);

            return RedirectToAction(nameof(Details),
                new { id = classroomId });
        }
    }
}