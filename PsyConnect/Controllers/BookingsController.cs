using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PsyConnect.Data;
using PsyConnect.Filters;
using PsyConnect.Models;
using PsyConnect.Services;
using PsyConnect.ViewModels;

namespace PsyConnect.Controllers
{
    [Authorize]
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IBookingStatusService _bookingStatusService;

        public BookingsController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IBookingStatusService bookingStatusService)
        {
            _context = context;
            _userManager = userManager;
            _bookingStatusService = bookingStatusService;
        }

        // GET: Bookings
        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10)
        {
            IQueryable<Booking> query;

            if (User.IsInRole("Admin"))
            {
                query = _context.Bookings
                    .Include(b => b.User)
                    .Include(b => b.Review)
                    .OrderByDescending(b => b.dateTime);
            }
            else
            {
                var userId = _userManager.GetUserId(User);

                query = _context.Bookings
                    .Where(b => b.UserId == userId)        // ✅ REQUIRED
                    .Include(b => b.User)
                    .Include(b => b.Review)               // ✅ so UI knows reviewed or not
                    .OrderByDescending(b => b.dateTime);
            }

            var allBookings = await query.ToListAsync();

            _bookingStatusService.UpdateStatus(allBookings);
            await _context.SaveChangesAsync();

            var totalItems = allBookings.Count;

            var bookingsPage = allBookings
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var vm = new BookingIndexVM
            {
                Bookings = bookingsPage,
                PageNumber = pageNumber,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };

            return View(vm);
        }


        // GET: Bookings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Review)
                .Include(b => b.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (booking == null) return NotFound();

            if (!User.IsInRole("Admin"))
            {
                var userId = _userManager.GetUserId(User);
                if (booking.UserId != userId) return Forbid();
            }

            return View(booking);
        }


        // GET: Bookings/Create
        [Authorize(Roles = "Patient")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Bookings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Patient")]
        [ServiceFilter(typeof(BookingEmailFilter))]
        public async Task<IActionResult> Create(
            [Bind("Title,Description,Type")] Booking booking,
            DateTime BookingDate,
            string SelectedTime)
        {
            // Basic check: did JS / user actually select both?
            if (BookingDate == default || string.IsNullOrWhiteSpace(SelectedTime))
            {
                ModelState.AddModelError("dateTime", "Please select a date and a time slot.");
                return View(booking);
            }

            // Parse the time slot ("09:00", "14:00", etc.)
            if (!TimeSpan.TryParse(SelectedTime, out var timeOfDay))
            {
                ModelState.AddModelError("dateTime", "Invalid time slot selected.");
                return View(booking);
            }

            // Combine date + time into one DateTime
            var dateValue = BookingDate.Date + timeOfDay;
            var now = DateTime.Now;

            // 1) No past dates/times
            if (dateValue <= now)
            {
                ModelState.AddModelError("dateTime", "You cannot select a past date or time.");
                return View(booking);
            }

            // 2) Only allow fixed time slots
            var allowedHours = new[] { 9, 10, 11, 12, 14, 15, 16, 17 };
            if (!allowedHours.Contains(dateValue.Hour) || dateValue.Minute != 0)
            {
                ModelState.AddModelError("dateTime",
                    "Please choose one of the allowed time slots: 9:00, 10:00, 11:00, 14:00, 15:00, or 16:00.");
                return View(booking);
            }

            // 3) Prevent double-booking the same slot
            bool slotTaken = await _context.Bookings
                .AnyAsync(b => b.dateTime == dateValue);

            if (slotTaken)
            {
                ModelState.AddModelError(string.Empty, "This time slot is already booked. Please choose another one.");
                return View(booking);
            }

            var userId = _userManager.GetUserId(User);

            // Fill Booking entity
            booking.UserId = userId;
            booking.dateTime = dateValue;

            var lastNumber = await _context.Bookings
                .Where(b => b.UserId == userId)
                .MaxAsync(b => (int?)b.Number) ?? 0;

            booking.Number = lastNumber + 1;
            booking.Status = "Pending";

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Bookings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && booking.UserId != userId)
                return Forbid();

            return View(booking);
        }

        // POST: Bookings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Patient")]
        [ServiceFilter(typeof(BookingEmailFilter))]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Title,Description,Type")] Booking formModel,
            DateTime BookingDate,
            string SelectedTime)
        {
            // 0) Route id check
            if (id != formModel.Id)
                return NotFound();

            // 1) Load existing booking via primary key
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
                return NotFound();

            // 2) Any user can reschedule HIS meeting, admin can edit all
            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && booking.UserId != userId)
                return Forbid();

            var today = DateTime.Today;

            if (booking.Status == "InProgress" ||
                booking.Status == "Completed" ||
                (booking.Status == "Pending" && booking.dateTime.Date == today))
                return Forbid();
            

                // ---------- SAME DATE/TIME LOGIC AS CREATE ----------

                // Basic check: date + time required
                if (BookingDate == default || string.IsNullOrWhiteSpace(SelectedTime))
            {
                ModelState.AddModelError("dateTime", "Please select a date and a time slot.");
                return View(booking);
            }

            // Parse timeslot ("09:00", "14:00"...)
            if (!TimeSpan.TryParse(SelectedTime, out var timeOfDay))
            {
                ModelState.AddModelError("dateTime", "Invalid time slot selected.");
                return View(booking);
            }

            var dateValue = BookingDate.Date + timeOfDay;
            var now = DateTime.Now;

            // 1) No past dates/times
            if (dateValue <= now)
            {
                ModelState.AddModelError("dateTime", "You cannot select a past date or time.");
                return View(booking);
            }

            // 2) Only allow fixed time slots
            var allowedHours = new[] { 9, 10, 11, 12, 14, 15, 16, 17 };
            if (!allowedHours.Contains(dateValue.Hour) || dateValue.Minute != 0)
            {
                ModelState.AddModelError(
                    "dateTime",
                    "Please choose one of the allowed time slots: 9:00, 10:00, 11:00, 14:00, 15:00, or 16:00."
                );
                return View(booking);
            }

            // 3) Prevent double booking (exclude this booking itself)
            bool slotTaken = await _context.Bookings
                .AnyAsync(b => b.Id != booking.Id && b.dateTime == dateValue);

            if (slotTaken)
            {
                ModelState.AddModelError(string.Empty, "This time slot is already booked. Please choose another one.");
                return View(booking);
            }

            // ---------- APPLY CHANGES ----------
            booking.Title = formModel.Title;
            booking.Description = formModel.Description;
            booking.Type = formModel.Type;

            // UserId + Number stay as they are (session count)
            booking.dateTime = dateValue;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Bookings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(m => m.Id == id);
            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // POST: Bookings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                _context.Bookings.Remove(booking);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ================== CALENDAR EVENTS FOR ADMIN ==================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminEvents(string? status = null)
        {
            var query = _context.Bookings
                .Include(b => b.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                query = query.Where(b => b.Status == status);
            }

            var events = await query
                .Select(b => new
                {
                    id = b.Id,
                    title = b.Title + " - " + (b.User != null ? b.User.Email : ""),
                    start = b.dateTime.ToString("o"),
                    extendedProps = new
                    {
                        type = b.Type,
                        status = b.Status,
                        number = b.Number
                    },
                    // simple coloring by status
                    backgroundColor = b.Status == "Completed" ? "#4caf50" :
                                      b.Status == "InProgress" ? "#ff9800" :
                                      "#2196f3",
                    borderColor = "#ffffff"
                })
                .ToListAsync();

            return Json(events);
        }

        // ================== CALENDAR EVENTS FOR PATIENT ==================
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> MyEvents(string? status = null)
        {
            var userId = _userManager.GetUserId(User);

            var query = _context.Bookings
                .Where(b => b.UserId == userId);

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                query = query.Where(b => b.Status == status);
            }

            var events = await query
                .Select(b => new
                {
                    id = b.Id,
                    title = b.Title + " (" + b.Type + ")",
                    start = b.dateTime.ToString("o"),
                    extendedProps = new
                    {
                        status = b.Status,
                        number = b.Number
                    },
                    backgroundColor = b.Status == "Completed" ? "#4caf50" :
                                      b.Status == "InProgress" ? "#ff9800" :
                                      "#2196f3",
                    borderColor = "#ffffff"
                })
                .ToListAsync();

            return Json(events);
        }

        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(e => e.Id == id);
        }
    }
}