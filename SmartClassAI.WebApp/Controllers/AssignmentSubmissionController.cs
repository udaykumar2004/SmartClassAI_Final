using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartClassAI.DataAccess.Repository.IRepository;
using SmartClassAI.Models;
using SmartClassAI.WebApp.Services;
using SmartClassAI.WebApp.Utility;
using System.Security.Claims;

namespace SmartClassAI.WebApp.Controllers
{
    [Authorize]
    public class AssignmentSubmissionController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ClassroomAccessService _access;
        private readonly FileUploadService _files;

        public AssignmentSubmissionController(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            ClassroomAccessService access,
            FileUploadService files)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _access = access;
            _files = files;
        }

        public async Task<IActionResult> Index(int assignmentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var assignment = await _unitOfWork.Assignment.GetAsync(
                x => x.Id == assignmentId,
                includeProperties: "Classroom");

            if (assignment?.Classroom == null)
                return NotFound();

            if (!await _access.CanGradeAssignmentAsync(user, assignment.ClassroomId))
                return Forbid();

            var submissions = await _unitOfWork.AssignmentSubmission.GetAllAsync(
                x => x.AssignmentId == assignmentId,
                includeProperties: "Student,Assignment");

            ViewBag.AssignmentId = assignmentId;
            ViewBag.AssignmentTitle = assignment.Title;
            return View(submissions);
        }

        public async Task<IActionResult> Submit(int assignmentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var assignment = await _unitOfWork.Assignment.GetAsync(
                x => x.Id == assignmentId,
                includeProperties: "Classroom");

            if (assignment?.Classroom == null)
                return NotFound();

            if (!await _access.CanSubmitAssignmentAsync(user, assignment.ClassroomId))
                return Forbid();

            var existing = await _unitOfWork.AssignmentSubmission.GetAsync(
                x => x.AssignmentId == assignmentId && x.StudentId == user.Id);

            if (existing != null)
                return View(existing);

            return View(new AssignmentSubmission { AssignmentId = assignmentId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(AssignmentSubmission obj, IFormFile? file)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var assignment = await _unitOfWork.Assignment.GetAsync(
                x => x.Id == obj.AssignmentId,
                includeProperties: "Classroom");

            if (assignment?.Classroom == null)
                return NotFound();

            if (!await _access.CanSubmitAssignmentAsync(user, assignment.ClassroomId))
                return Forbid();

            obj.StudentId = user.Id;

            var existing = await _unitOfWork.AssignmentSubmission.GetAsync(
                x => x.AssignmentId == obj.AssignmentId && x.StudentId == user.Id);

            if (file != null)
            {
                var (url, error) = await _files.UploadAsync(
                    file,
                    SD.SubmissionFile_Folder,
                    SD.Allowed_Submission_Extensions,
                    SD.MaxFileSize_Bytes);

                if (error != null)
                {
                    TempData["error"] = error;
                    return View(obj);
                }

                obj.SubmissionFileUrl = url;
            }

            var status = DateTime.UtcNow > assignment.DueDate
                ? SD.Submission_Late
                : SD.Submission_Submitted;

            if (existing == null)
            {
                obj.Status = status;
                obj.SubmittedAt = DateTime.UtcNow;
                _unitOfWork.AssignmentSubmission.Add(obj);
            }
            else
            {
                existing.SubmissionText = obj.SubmissionText;

                if (!string.IsNullOrEmpty(obj.SubmissionFileUrl))
                {
                    _files.DeleteIfExists(existing.SubmissionFileUrl);
                    existing.SubmissionFileUrl = obj.SubmissionFileUrl;
                }

                existing.Status = status;
                existing.SubmittedAt = DateTime.UtcNow;
                _unitOfWork.AssignmentSubmission.Update(existing);
            }

            await _unitOfWork.SaveAsync();

            TempData["success"] = "Submission saved successfully.";
            return RedirectToAction("Details", "Assignment", new { id = obj.AssignmentId });
        }

        public async Task<IActionResult> Review(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var submission = await _unitOfWork.AssignmentSubmission.GetAsync(
                x => x.Id == id,
                includeProperties: "Student,Assignment,Assignment.Classroom");

            if (submission?.Assignment?.Classroom == null)
                return NotFound();

            if (!await _access.CanGradeAssignmentAsync(user, submission.Assignment.ClassroomId))
                return Forbid();

            return View(submission);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(AssignmentSubmission obj)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var db = await _unitOfWork.AssignmentSubmission.GetAsync(
                x => x.Id == obj.Id,
                includeProperties: "Assignment,Assignment.Classroom");

            if (db?.Assignment?.Classroom == null)
                return NotFound();

            if (!await _access.CanGradeAssignmentAsync(user, db.Assignment.ClassroomId))
                return Forbid();

            if (obj.Marks.HasValue && obj.Marks > db.Assignment.TotalMarks)
            {
                ModelState.AddModelError("Marks", $"Marks cannot exceed {db.Assignment.TotalMarks}.");
                return View(db);
            }

            db.Marks = obj.Marks;
            db.Feedback = obj.Feedback;
            db.Status = SD.Submission_Reviewed;
            _unitOfWork.AssignmentSubmission.Update(db);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Submission graded successfully.";
            return RedirectToAction(nameof(Index), new { assignmentId = db.AssignmentId });
        }

        public async Task<IActionResult> Download(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var submission = await _unitOfWork.AssignmentSubmission.GetAsync(
                x => x.Id == id,
                includeProperties: "Assignment,Assignment.Classroom");

            if (submission == null || string.IsNullOrEmpty(submission.SubmissionFileUrl))
                return NotFound();

            bool canGrade = await _access.CanGradeAssignmentAsync(
                user, submission.Assignment!.ClassroomId);

            bool isOwner = submission.StudentId == user.Id;

            if (!canGrade && !isOwner)
                return Forbid();

            var (stream, contentType, fileName) = _files.GetDownloadStream(submission.SubmissionFileUrl);
            if (stream == null)
                return NotFound();

            return File(stream, contentType, fileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var submission = await _unitOfWork.AssignmentSubmission.GetAsync(
                x => x.Id == id,
                includeProperties: "Assignment,Assignment.Classroom");

            if (submission?.Assignment == null)
                return NotFound();

            if (!await _access.CanGradeAssignmentAsync(user, submission.Assignment.ClassroomId))
                return Forbid();

            _files.DeleteIfExists(submission.SubmissionFileUrl);

            int assignmentId = submission.AssignmentId;
            _unitOfWork.AssignmentSubmission.Remove(submission);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Submission deleted.";
            return RedirectToAction(nameof(Index), new { assignmentId });
        }
    }
}
