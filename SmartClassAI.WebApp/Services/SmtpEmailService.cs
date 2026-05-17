using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using MimeKit;

namespace SmartClassAI.WebApp.Services
{
    public class SmtpEmailService : IEmailSender
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(
            IConfiguration config,
            ILogger<SmtpEmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendEmailAsync(
    string email,
    string subject,
    string htmlMessage)
        {
            try
            {
                var message = new MimeMessage();

                message.From.Add(
                    new MailboxAddress(
                        _config["Email:SenderName"],
                        _config["Email:SenderEmail"]));

                message.To.Add(MailboxAddress.Parse(email));

                message.Subject = subject;

                var builder = new BodyBuilder
                {
                    HtmlBody = htmlMessage
                };

                message.Body = builder.ToMessageBody();

                using var smtp = new SmtpClient();

                await smtp.ConnectAsync(
    "smtp.gmail.com",
    465,
    SecureSocketOptions.SslOnConnect);

                await smtp.AuthenticateAsync(
                    _config["Email:Username"],
                    _config["Email:Password"]);

                await smtp.SendAsync(message);

                await smtp.DisconnectAsync(true);

                _logger.LogInformation(
                    "Email sent successfully to {Email}",
                    email);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send email to {Email}",
                    email);

                // TEMPORARY FOR DEBUGGING
                throw;
            }
        }
    }
}