using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using PsyConnect.Data;
using PsyConnect.Models;
using System.Security.Cryptography;

namespace PsyConnect.Services
{
    public interface IEmailOTPService
    {
        Task GenerateAndSendOtpAsync(IdentityUser user);
        Task<bool> VerifyOtpAsync(IdentityUser user, string code);
    }

    public class EmailOTPService : IEmailOTPService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;

        public EmailOTPService(ApplicationDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        public async Task GenerateAndSendOtpAsync(IdentityUser user)
        {
            // 6-digit random code
            var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

            // invalidate previous codes for this user
            var oldCodes = await _context.EmailOTPCodes
                .Where(x => x.UserId == user.Id && !x.IsUsed)
                .ToListAsync();

            foreach (var c in oldCodes)
                c.IsUsed = true;

            var otp = new EmailOTPCode
            {
                UserId = user.Id,
                Code = code,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                IsUsed = false
            };

            _context.EmailOTPCodes.Add(otp);
            await _context.SaveChangesAsync();

            var subject = "PsyConnect – Email verification code";
            var body = $@"
<h2>Verify your email 🔐</h2>
<p>Hello {user.UserName},</p>
<p>Your verification code is:</p>
<h1 style='letter-spacing:4px'>{code}</h1>
<p>This code will expire in 10 minutes.</p>";

            await _emailSender.SendEmailAsync(user.Email!, subject, body);
        }

        public async Task<bool> VerifyOtpAsync(IdentityUser user, string code)
        {
            var otp = await _context.EmailOTPCodes
                .Where(x => x.UserId == user.Id
                            && x.Code == code
                            && !x.IsUsed
                            && x.ExpiresAt >= DateTime.UtcNow)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            if (otp == null)
                return false;

            otp.IsUsed = true;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
