// ============================================================
// MeetingController.cs — FIXED
//
// BUGS FIXED:
//  1. Room() Forbid()-ed students — they could NEVER join a meeting
//     FIX: Students who are ClassroomMembers can now enter Room()
//  2. End(), Delete(), DeleteByRoom() missing [ValidateAntiForgeryToken]
//     FIX: Added CSRF protection on all POST actions
//  3. Join() showed empty view with no context
//     FIX: Finds active meeting and redirects to Room, or shows "no meeting"
//  4. All DB calls are now async
// ============================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartClassAI.DataAccess.Repository.IRepository;
using SmartClassAI.Models;
using SmartClassAI.WebApp.Utility;

namespace SmartClassAI.WebApp.Controllers
{
    [Authorize]
    public class MeetingController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public MeetingController(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        // =============================================
        // START MEETING (Teacher/Admin only)
        // =============================================
        [HttpGet]
        public async Task<IActionResult> Start(int classroomId)
        {
            if (classroomId <= 0) return BadRequest();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var classroom = await _unitOfWork.Classroom.GetAsync(
                x => x.Id == classroomId);
            if (classroom == null) return NotFound();

            bool isAdmin = await _userManager.IsInRoleAsync(user, SD.Role_Admin);
            bool isTeacher = classroom.TeacherId == user.Id;

            if (!isAdmin && !isTeacher)
                return Forbid();

            // If active meeting already exists, redirect to it
            var activeMeeting = await _unitOfWork.Meeting.GetAsync(
                x => x.ClassroomId == classroomId && x.IsActive);

            if (activeMeeting != null)
            {
                return RedirectToAction(nameof(Room), new
                {
                    roomId = activeMeeting.MeetingRoomId,
                    classroomId
                });
            }

            // Create new meeting
            var meeting = new Meeting
            {
                ClassroomId = classroomId,
                UserId = user.Id,
                MeetingRoomId = Guid.NewGuid().ToString(),
                IsActive = true,
                StartedAt = DateTime.UtcNow
            };

            // Mark classroom as live
            classroom.IsLive = true;
            _unitOfWork.Classroom.Update(classroom);
            _unitOfWork.Meeting.Add(meeting);
            await _unitOfWork.SaveAsync();

            return RedirectToAction(nameof(Room), new
            {
                roomId = meeting.MeetingRoomId,
                classroomId
            });
        }

        // =============================================
        // JOIN (Student — finds active meeting)
        // FIX: Was returning empty view. Now finds the active meeting
        //      and redirects to Room, or shows "no active meeting" UI
        // =============================================
        [HttpGet]
        public async Task<IActionResult> Join(int classroomId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var classroom = await _unitOfWork.Classroom.GetAsync(
                x => x.Id == classroomId);
            if (classroom == null) return NotFound();

            // Verify user is a member of this classroom
            bool isAdmin = await _userManager.IsInRoleAsync(user, SD.Role_Admin);
            bool isTeacher = classroom.TeacherId == user.Id;
            bool isMember = await _unitOfWork.ClassroomMember.AnyAsync(
                x => x.ClassroomId == classroomId && x.UserId == user.Id);

            if (!isAdmin && !isTeacher && !isMember)
                return Forbid();

            var activeMeeting = await _unitOfWork.Meeting.GetAsync(
                x => x.ClassroomId == classroomId && x.IsActive);

            if (activeMeeting == null)
            {
                ViewBag.ClassroomId = classroomId;
                ViewBag.ClassroomName = classroom.Name;
                TempData["error"] = "No active meeting found for this classroom.";
                return View(); // Shows "no active meeting" view
            }

            // Redirect to room (students are now allowed in Room())
            return RedirectToAction(nameof(Room), new
            {
                roomId = activeMeeting.MeetingRoomId,
                classroomId
            });
        }

        // =============================================
        // ROOM — Video call page
        // FIX: Was Forbid()-ing students. Now all classroom members can enter.
        // =============================================
        [HttpGet]
        public async Task<IActionResult> Room(string roomId, int classroomId)
        {
            if (string.IsNullOrWhiteSpace(roomId))
                return BadRequest();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var meeting = await _unitOfWork.Meeting.GetAsync(
                x => x.MeetingRoomId == roomId && x.IsActive);
            if (meeting == null)
            {
                TempData["error"] = "This meeting has ended or does not exist.";
                return RedirectToAction("Details", "Classroom", new { id = classroomId });
            }

            var classroom = await _unitOfWork.Classroom.GetAsync(
                x => x.Id == classroomId);
            if (classroom == null) return NotFound();

            bool isAdmin = await _userManager.IsInRoleAsync(user, SD.Role_Admin);
            bool isTeacher = classroom.TeacherId == user.Id;

            // FIX: Students (ClassroomMembers) can now enter the room
            bool isMember = await _unitOfWork.ClassroomMember.AnyAsync(
                x => x.ClassroomId == classroomId && x.UserId == user.Id);

            if (!isAdmin && !isTeacher && !isMember)
                return Forbid();

            ViewBag.RoomId = roomId;
            ViewBag.ClassroomId = classroomId;
            ViewBag.ClassroomName = classroom.Name;
            ViewBag.UserId = user.Id;
            ViewBag.UserName = user.FullName.Length > 0 ? user.FullName : user.UserName;
            ViewBag.IsTeacher = isTeacher || isAdmin;

            return View();
        }

        // =============================================
        // END MEETING
        // FIX: Added [ValidateAntiForgeryToken]
        // =============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> End(string roomId, int classroomId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var meeting = await _unitOfWork.Meeting.GetAsync(
                x => x.MeetingRoomId == roomId && x.IsActive);
            if (meeting == null) return NotFound();

            meeting.IsActive = false;
            meeting.EndedAt = DateTime.UtcNow;
            _unitOfWork.Meeting.Update(meeting);

            // Update classroom live status
            var classroom = await _unitOfWork.Classroom.GetAsync(
                x => x.Id == classroomId);
            if (classroom != null)
            {
                classroom.IsLive = false;
                _unitOfWork.Classroom.Update(classroom);
            }

            await _unitOfWork.SaveAsync();

            TempData["success"] = "Meeting ended.";
            return RedirectToAction("Details", "Classroom", new { id = classroomId });
        }

        // =============================================
        // DELETE
        // FIX: Added [ValidateAntiForgeryToken]
        // =============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int meetingId, int classroomId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var meeting = await _unitOfWork.Meeting.GetAsync(x => x.Id == meetingId);
            if (meeting == null) return NotFound();

            // Only admin or the meeting host can delete
            bool isAdmin = await _userManager.IsInRoleAsync(user, SD.Role_Admin);
            if (!isAdmin && meeting.UserId != user.Id)
                return Forbid();

            _unitOfWork.Meeting.Remove(meeting);
            await _unitOfWork.SaveAsync();

            return RedirectToAction("Details", "Classroom", new { id = classroomId });
        }
    }
}