using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PsyConnect.Data;
using PsyConnect.Models;

namespace PsyConnect.Controllers
{
    [Authorize]
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public BookingsController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Bookings
        public async Task<IActionResult> Index()
        {
            // Admin sees everything
            if (User.IsInRole("Admin"))
            {
                var allBookings = await _context.Bookings
                    .Include(b => b.User)
                    .ToListAsync();
                return View(allBookings);
            }

            // Patient: only his own bookings
            var userId = _userManager.GetUserId(User);

            var myBookings = await _context.Bookings
                .Where(b => b.UserId == userId)
                .ToListAsync();

            return View(myBookings);
        }


        // GET: Bookings/Details/5
        public async Task<IActionResult> Details(int? id)
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
        public async Task<IActionResult> Create(Booking booking)
        {
            // attach current user
            booking.UserId = _userManager.GetUserId(User);

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
        public async Task<IActionResult> Edit(int id, Booking booking)
        {
            if (id != booking.Id) return NotFound();

            var existingBooking = await _context.Bookings.FindAsync(id);
            if (existingBooking == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && existingBooking.UserId != userId)
                return Forbid();

            // update fields
            existingBooking.Title = booking.Title;
            existingBooking.Description = booking.Description;
            existingBooking.Number = booking.Number;
            existingBooking.Type = booking.Type;
            existingBooking.Status = booking.Status;
            existingBooking.dateTime = booking.dateTime;

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

        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(e => e.Id == id);
        }
    }
}
