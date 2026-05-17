using Microsoft.AspNetCore.Identity.UI.Services;

namespace SmartClassAI.Utility;

public class EmailSender : IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        // Email logic later
        return Task.CompletedTask;
    }
}