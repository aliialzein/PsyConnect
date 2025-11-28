using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PsyConnect.Data;
using PsyConnect.Models;
using System.Linq;

namespace PsyConnect.Filters
{
    public class BookingEmailFilter : IAsyncActionFilter
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<BookingEmailFilter> _logger;

        public BookingEmailFilter(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IEmailSender emailSender,
            ILogger<BookingEmailFilter> logger)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            // 1) Execute the action (create / edit)
            var executedContext = await next();

            // If action threw, don't send emails
            if (executedContext.Exception != null && !executedContext.ExceptionHandled)
            {
                _logger.LogWarning("BookingEmailFilter skipped because action threw an exception.");
                return;
            }

            if (executedContext.Controller is not Controller controller)
            {
                _logger.LogWarning("BookingEmailFilter: controller is not MVC Controller – skipping.");
                return;
            }

            var userId = _userManager.GetUserId(controller.User);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("BookingEmailFilter: no user id – skipping.");
                return;
            }

            var controllerName = controller.RouteData.Values["controller"]?.ToString() ?? "";
            var actionName = controller.RouteData.Values["action"]?.ToString() ?? "";

            bool isEdit =
                string.Equals(controllerName, "Bookings", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(actionName, "Edit", StringComparison.OrdinalIgnoreCase);

            // 2) Get the correct booking
            Booking? booking = null;

            if (isEdit &&
                context.ActionArguments.TryGetValue("id", out var idObj) &&
                idObj is int idValue)
            {
                // Edit: use the booking being edited
                booking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.Id == idValue && b.UserId == userId);
            }
            else
            {
                // Create (via Payments / Create): use the latest booking for this user
                booking = await _context.Bookings
                    .Where(b => b.UserId == userId)
                    .OrderByDescending(b => b.Id) // most recently inserted row
                    .FirstOrDefaultAsync();
            }

            if (booking == null)
            {
                _logger.LogWarning("BookingEmailFilter: no booking found for user {UserId}.", userId);
                return;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.Email))
            {
                _logger.LogWarning("BookingEmailFilter: user or email missing for {UserId}.", userId);
                return;
            }

            // ========= EMAIL TO USER =========

            var userSubject = isEdit
                ? "PsyConnect – Booking Updated"
                : "PsyConnect – Booking Confirmation";

            var userBody = isEdit
                ? $@"
<h2>Your session was updated 📝</h2>
<p>Hello {user.UserName},</p>
<p>Your booking has been successfully updated. Here are the new details:</p>
<ul>
  <li><strong>Title:</strong> {booking.Title}</li>
  <li><strong>Type:</strong> {booking.Type}</li>
  <li><strong>Date &amp; Time:</strong> {booking.dateTime:dddd, MMMM d, yyyy h:mm tt}</li>
  <li><strong>Status:</strong> {booking.Status}</li>
  <li><strong>Queue Number:</strong> {booking.Number}</li>
</ul>
<p>Thank you for using PsyConnect.</p>"
                : $@"
<h2>Your session is booked ✅</h2>
<p>Hello {user.UserName},</p>
<p>Your booking has been successfully created. Here are your session details:</p>
<ul>
  <li><strong>Title:</strong> {booking.Title}</li>
  <li><strong>Type:</strong> {booking.Type}</li>
  <li><strong>Date &amp; Time:</strong> {booking.dateTime:dddd, MMMM d, yyyy h:mm tt}</li>
  <li><strong>Status:</strong> {booking.Status}</li>
  <li><strong>Queue Number:</strong> {booking.Number}</li>
</ul>
<p>Thank you for using PsyConnect.</p>";

            await _emailSender.SendEmailAsync(user.Email, userSubject, userBody);

            _logger.LogInformation(
                "✅ BookingEmailFilter: User email sent to {Email} for booking {BookingId}.",
                user.Email, booking.Id);

            // ========= EMAIL TO ADMIN =========

            try
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                var admin = admins.FirstOrDefault();

                if (admin == null || string.IsNullOrEmpty(admin.Email))
                {
                    _logger.LogWarning("BookingEmailFilter: no admin user with email found.");
                    return;
                }

                var adminSubject = isEdit
                    ? "PsyConnect – Booking Updated by Patient"
                    : "PsyConnect – New Booking Created";

                var adminBody = isEdit
                    ? $@"
<h2>Booking updated 📝</h2>
<p>The following booking was updated by a patient.</p>
<ul>
  <li><strong>Patient:</strong> {user.UserName} ({user.Email})</li>
  <li><strong>Title:</strong> {booking.Title}</li>
  <li><strong>Type:</strong> {booking.Type}</li>
  <li><strong>Date &amp; Time:</strong> {booking.dateTime:dddd, MMMM d, yyyy h:mm tt}</li>
  <li><strong>Status:</strong> {booking.Status}</li>
  <li><strong>Queue Number:</strong> {booking.Number}</li>
</ul>"
                    : $@"
<h2>New booking created ✅</h2>
<p>A new booking was created by a patient.</p>
<ul>
  <li><strong>Patient:</strong> {user.UserName} ({user.Email})</li>
  <li><strong>Title:</strong> {booking.Title}</li>
  <li><strong>Type:</strong> {booking.Type}</li>
  <li><strong>Date &amp; Time:</strong> {booking.dateTime:dddd, MMMM d, yyyy h:mm tt}</li>
  <li><strong>Status:</strong> {booking.Status}</li>
  <li><strong>Queue Number:</strong> {booking.Number}</li>
</ul>";

                await _emailSender.SendEmailAsync(admin.Email, adminSubject, adminBody);

                _logger.LogInformation(
                    "✅ BookingEmailFilter: Admin email sent to {Email} for booking {BookingId}.",
                    admin.Email, booking.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingEmailFilter: error while sending admin email.");
            }
        }
    }
}
