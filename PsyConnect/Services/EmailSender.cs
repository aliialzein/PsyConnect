using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using PsyConnect.Models;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace PsyConnect.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly SmtpSettings _smtp;

        public EmailSender(IOptions<SmtpSettings> smtp)
        {
            _smtp = smtp.Value;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            using var client = new SmtpClient(_smtp.Host, _smtp.Port)
            {
                Credentials = new NetworkCredential(_smtp.UserName, _smtp.Password),
                EnableSsl = _smtp.EnableSsl
            };

            var mail = new MailMessage
            {
                From = new MailAddress(_smtp.FromEmail, _smtp.FromName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            mail.To.Add(email);

            await client.SendMailAsync(mail);
        }
    }
}
