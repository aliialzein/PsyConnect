using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PsyConnect.Data;
using PsyConnect.Models;
using System.Text;

namespace PsyConnect.Services
{
    public class BookingReminderService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BookingReminderService> _logger;

        public BookingReminderService(
            IServiceScopeFactory scopeFactory,
            ILogger<BookingReminderService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("📧 BookingReminderService started.");

            bool adminSummarySentToday = false;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                    var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                    var now = DateTime.Now;

                    // ================= PATIENT REMINDERS =================
                    var tomorrow = DateTime.Today.AddDays(1);
                    var patientBookings = await context.Bookings
                        .Where(b =>
                            b.Status == "Pending" &&
                            b.dateTime.Date == tomorrow &&
                            !b.PatientReminderSent)
                        .ToListAsync(stoppingToken);

                    foreach (var booking in patientBookings)
                    {
                        var user = await userManager.FindByIdAsync(booking.UserId);
                        if (user == null || string.IsNullOrEmpty(user.Email))
                            continue;

                        var subject = "PsyConnect – Reminder for your session tomorrow";
                        var body = $@"
<h2>Session reminder ⏰</h2>
<p>Hello {user.UserName},</p>
<p>This is a reminder of your session scheduled for <strong>tomorrow</strong>:</p>
<ul>
  <li><strong>Title:</strong> {booking.Title}</li>
  <li><strong>Type:</strong> {booking.Type}</li>
  <li><strong>Date &amp; Time:</strong> {booking.dateTime:dddd, MMMM d, yyyy h:mm tt}</li>
  <li><strong>Status:</strong> {booking.Status}</li>
  <li><strong>Queue Number:</strong> {booking.Number}</li>
</ul>
<p>Please make sure to be available on time.</p>";

                        await emailSender.SendEmailAsync(user.Email, subject, body);
                        booking.PatientReminderSent = true;

                        _logger.LogInformation(
                            "📧 Patient reminder sent to {Email} for booking {BookingId}.",
                            user.Email, booking.Id);
                    }

                    // ================= ADMIN DAILY SUMMARY =================
                    // e.g., send once per day around 08:00
                    if (now.Hour == 8 && !adminSummarySentToday)
                    {
                        var today = DateTime.Today;

                        var todaysBookings = await context.Bookings
                            .Where(b => b.dateTime.Date == today)
                            .OrderBy(b => b.dateTime)
                            .ToListAsync(stoppingToken);

                        if (todaysBookings.Any())
                        {
                            var admins = await userManager.GetUsersInRoleAsync("Admin");

                            var sb = new StringBuilder();
                            sb.Append("<h2>Today's bookings 📅</h2>");
                            sb.Append($"<p>Date: <strong>{today:dddd, MMMM d, yyyy}</strong></p>");
                            sb.Append("<table border='1' cellspacing='0' cellpadding='4'>");
                            sb.Append("<tr><th>Time</th><th>Patient</th><th>Title</th><th>Type</th><th>Status</th><th>#</th></tr>");

                            foreach (var booking in todaysBookings)
                            {
                                var patient = await userManager.FindByIdAsync(booking.UserId);
                                var patientName = patient?.UserName ?? "Unknown";

                                sb.Append("<tr>");
                                sb.Append($"<td>{booking.dateTime:HH:mm}</td>");
                                sb.Append($"<td>{patientName}</td>");
                                sb.Append($"<td>{booking.Title}</td>");
                                sb.Append($"<td>{booking.Type}</td>");
                                sb.Append($"<td>{booking.Status}</td>");
                                sb.Append($"<td>{booking.Number}</td>");
                                sb.Append("</tr>");
                            }

                            sb.Append("</table>");

                            foreach (var admin in admins.Where(a => !string.IsNullOrEmpty(a.Email)))
                            {
                                await emailSender.SendEmailAsync(
                                    admin.Email,
                                    "PsyConnect – Today's bookings summary",
                                    sb.ToString());

                                _logger.LogInformation(
                                    "📧 Admin daily summary sent to {Email}.",
                                    admin.Email);
                            }
                        }

                        adminSummarySentToday = true;
                    }

                    // Reset flag after midnight
                    if (DateTime.Now.Hour == 0)
                    {
                        adminSummarySentToday = false;
                    }

                    await context.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in BookingReminderService loop.");
                }

                // Run every 30 minutes (you can change this)
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }

            _logger.LogInformation("📧 BookingReminderService stopping.");
        }
    }
}
