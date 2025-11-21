using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace PsyConnect.Services
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // For now do nothing or log to console / debug window.
            // This keeps Identity happy without sending real emails.
            return Task.CompletedTask;
        }
    }
}
