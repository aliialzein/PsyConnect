using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PsyConnect.Data;
using PsyConnect.Models;
using Stripe.Checkout;

namespace PsyConnect.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly StripeSettings _stripeSettings;

        public PaymentsController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IOptions<StripeSettings> stripeOptions)
        {
            _context = context;
            _userManager = userManager;
            _stripeSettings = stripeOptions.Value;
        }

        // STEP 1: called from Booking Create form instead of BookingsController.Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(
            [Bind("Title,Description,Type")] Booking booking,
            DateTime BookingDate,
            string SelectedTime)
        {
            // ====== SAME VALIDATION AS YOUR BookingsController.Create ======

            if (BookingDate == default || string.IsNullOrWhiteSpace(SelectedTime))
            {
                ModelState.AddModelError("dateTime", "Please select a date and a time slot.");
                return View("~/Views/Bookings/Create.cshtml", booking);
            }

            if (!TimeSpan.TryParse(SelectedTime, out var timeOfDay))
            {
                ModelState.AddModelError("dateTime", "Invalid time slot selected.");
                return View("~/Views/Bookings/Create.cshtml", booking);
            }

            var dateValue = BookingDate.Date + timeOfDay;
            var now = DateTime.Now;

            if (dateValue <= now)
            {
                ModelState.AddModelError("dateTime", "You cannot select a past date or time.");
                return View("~/Views/Bookings/Create.cshtml", booking);
            }

            var allowedHours = new[] { 9, 10, 11, 12, 14, 15, 16, 17 };
            if (!allowedHours.Contains(dateValue.Hour) || dateValue.Minute != 0)
            {
                ModelState.AddModelError("dateTime",
                    "Please choose one of the allowed time slots: 9:00, 10:00, 11:00, 14:00, 15:00, or 16:00.");
                return View("~/Views/Bookings/Create.cshtml", booking);
            }

            bool slotTaken = await _context.Bookings
                .AnyAsync(b => b.dateTime == dateValue);

            if (slotTaken)
            {
                ModelState.AddModelError(string.Empty, "This time slot is already booked. Please choose another one.");
                return View("~/Views/Bookings/Create.cshtml", booking);
            }

            var userId = _userManager.GetUserId(User);

            decimal amount = booking.Type == "Onsite" ? 30m : 20m;

            var payment = new Payment
            {
                UserId = userId,
                Amount = amount,
                BookingTitle = booking.Title,
                BookingDescription = booking.Description,
                BookingType = booking.Type,
                BookingDateTime = dateValue,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // Step 2: choose payment method (Stripe or Dummy)
            return RedirectToAction(nameof(ChooseMethod), new { id = payment.Id });
        }

        // STEP 2: let the user pick Stripe or Dummy
        [HttpGet]
        public async Task<IActionResult> ChooseMethod(int id)
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
                return NotFound();

            return View(payment);
        }

        // ====================== DUMMY GATEWAY ======================

        [HttpGet]
        public async Task<IActionResult> DummyCheckout(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
                return NotFound();

            if (payment.Status == "Paid")
                return RedirectToAction("Index", "Bookings");

            return View("Checkout", payment); // reuse Checkout view
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DummyConfirm(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
                return NotFound();

            if (payment.Status == "Paid")
                return RedirectToAction("Index", "Bookings");

            // Re-check slot
            bool slotTaken = await _context.Bookings
                .AnyAsync(b => b.dateTime == payment.BookingDateTime);

            if (slotTaken)
            {
                payment.Status = "Failed";
                await _context.SaveChangesAsync();

                ModelState.AddModelError(string.Empty,
                    "Sorry, this time slot has just been booked by someone else. Please choose another time.");
                return View("Checkout", payment);
            }

            var lastNumber = await _context.Bookings
                .Where(b => b.UserId == payment.UserId)
                .MaxAsync(b => (int?)b.Number) ?? 0;

            var booking = new Booking
            {
                Title = payment.BookingTitle,
                Description = payment.BookingDescription,
                Type = payment.BookingType,
                dateTime = payment.BookingDateTime,
                UserId = payment.UserId,
                Number = lastNumber + 1,
                Status = "Pending"
            };

            _context.Bookings.Add(booking);
            payment.Status = "Paid";

            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Bookings");
        }

        // ====================== STRIPE CHECKOUT ======================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StripeCheckout(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
                return NotFound();

            var domain = $"{Request.Scheme}://{Request.Host}";

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                Mode = "payment",
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "usd",
                            UnitAmount = (long)(payment.Amount * 100), // cents
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Therapy Session - {payment.BookingType}"
                            }
                        }
                    }
                },
                SuccessUrl = $"{domain}/Payments/StripeSuccess?paymentId={payment.Id}",
                CancelUrl = $"{domain}/Payments/StripeCancel?paymentId={payment.Id}"
            };

            var service = new SessionService();
            var session = service.Create(options);

            return Redirect(session.Url);
        }

        [HttpGet]
        public async Task<IActionResult> StripeSuccess(int paymentId)
        {
            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment == null)
                return NotFound();

            if (payment.Status == "Paid")
                return RedirectToAction("Index", "Bookings");

            // Final slot check
            bool slotTaken = await _context.Bookings
                .AnyAsync(b => b.dateTime == payment.BookingDateTime);

            if (slotTaken)
            {
                payment.Status = "Failed";
                await _context.SaveChangesAsync();

                TempData["Error"] = "Sorry, this time slot has just been booked by someone else.";
                return RedirectToAction("Index", "Bookings");
            }

            var lastNumber = await _context.Bookings
                .Where(b => b.UserId == payment.UserId)
                .MaxAsync(b => (int?)b.Number) ?? 0;

            var booking = new Booking
            {
                Title = payment.BookingTitle,
                Description = payment.BookingDescription,
                Type = payment.BookingType,
                dateTime = payment.BookingDateTime,
                UserId = payment.UserId,
                Number = lastNumber + 1,
                Status = "Pending"
            };

            _context.Bookings.Add(booking);
            payment.Status = "Paid";

            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Bookings");
        }

        [HttpGet]
        public async Task<IActionResult> StripeCancel(int paymentId)
        {
            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment == null)
                return NotFound();

            payment.Status = "Canceled";
            await _context.SaveChangesAsync();

            TempData["Error"] = "Payment was canceled.";
            return RedirectToAction("Index", "Bookings");
        }
    }
}
