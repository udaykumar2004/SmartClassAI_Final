using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SmartClassAI.WebApp.Controllers;

/// <summary>
/// Legacy enrollment routes — redirects to the secure Classroom join flow.
/// </summary>
[Authorize]
public class EnrollmentController : Controller
{
    public IActionResult Join() =>
        RedirectToAction(nameof(ClassroomController.Join), "Classroom");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Join(string joinCode)
    {
        if (!string.IsNullOrWhiteSpace(joinCode))
            TempData["JoinCode"] = joinCode.Trim().ToUpper();

        return RedirectToAction(nameof(ClassroomController.Join), "Classroom");
    }

    public IActionResult Students(int classroomId) =>
        RedirectToAction(nameof(ClassroomController.Details), "Classroom", new { id = classroomId });

    public IActionResult Remove(int id) =>
        RedirectToAction(nameof(ClassroomController.Index), "Classroom");
}
