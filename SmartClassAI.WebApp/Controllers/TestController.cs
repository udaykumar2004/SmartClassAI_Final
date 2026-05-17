using Microsoft.AspNetCore.Mvc;
using SmartClassAI.WebApp.Services;

namespace SmartClassAI.WebApp.Controllers
{
    public class TestController : Controller
    {
        private readonly EmailTemplateService _email;

        public TestController(EmailTemplateService email)
        {
            _email = email;
        }

        public async Task<IActionResult> Send()
        {
            await _email.SendClassroomCreatedAsync(
                "uday.kaundal2004@gmail.com",
                "Uday",
                "AI Classroom",
                "Artificial Intelligence",
                "ABC123");

            return Content("Email Sent Successfully");
        }
    }
}