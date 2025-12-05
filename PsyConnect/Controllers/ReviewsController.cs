using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PsyConnect.Data;
using PsyConnect.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PsyConnect.Controllers
{
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public ReviewsController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        private bool IsAdmin() => User.IsInRole("Admin");

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var reviews = await _db.Reviews
                .Include(r => r.Booking)
                .OrderByDescending(r => r.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            return View(reviews);
        }

        // PATIENT: only his own reviews
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> My()
        {
            var userId = _userManager.GetUserId(User);

            var reviews = await _db.Reviews
                .Include(r => r.Booking)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            return View(reviews);
        }

        // DETAILS:
        // - Admin can view any review
        // - Patient can view only if review belongs to them (review.UserId == current user)
        public async Task<IActionResult> Details(int id)
        {
            var review = await _db.Reviews
                .Include(r => r.Booking)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);

            if (review == null) return NotFound();

            if (!IsAdmin())
            {
                var userId = _userManager.GetUserId(User);
                if (review.UserId != userId)
                    return Forbid();
            }

            return View(review);
        }

        // CREATE (GET): only Patient, only completed booking, only owner, one review per booking
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> Create(int bookingId)
        {
            var userId = _userManager.GetUserId(User);

            var booking = await _db.Bookings
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return NotFound();

            // owner check
            if (booking.UserId != userId)
                return Forbid();

            // only after completed
            if (booking.Status != "Completed")
                return Forbid();

            // one review per booking
            var existing = await _db.Reviews
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.BookingId == bookingId);

            if (existing != null)
                return RedirectToAction(nameof(Edit), new { id = existing.Id }); // nicer UX: go edit existing

            ViewBag.BookingTitle = booking.Title;
            ViewBag.BookingDateTime = booking.dateTime;

            return View(new Review
            {
                BookingId = bookingId,
                UserId = userId ?? "",
                Rating = 5
            });
        }

        // CREATE (POST): only Patient, only completed booking, only owner, one review per booking
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> Create(Review model)
        {
            // We set these server-side, so don't require them from the form
            ModelState.Remove(nameof(Review.UserId));
            ModelState.Remove(nameof(Review.CreatedAt));

            var userId = _userManager.GetUserId(User);
            model.UserId = userId ?? "";

            var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == model.BookingId);
            if (booking == null) return NotFound();

            if (booking.UserId != userId) return Forbid();
            if (booking.Status != "Completed") return Forbid();

            var already = await _db.Reviews.AnyAsync(r => r.BookingId == model.BookingId);
            if (already) ModelState.AddModelError("", "A review for this booking already exists.");

            if (!ModelState.IsValid)
            {
                ViewBag.BookingTitle = booking.Title;
                ViewBag.BookingDateTime = booking.dateTime;
                return View(model);
            }

            model.CreatedAt = DateTime.UtcNow;
            _db.Reviews.Add(model);
            await _db.SaveChangesAsync();

            return RedirectToAction("Index", "Bookings");
        }

        // EDIT (GET): only Patient, only review owner, and booking must be completed
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);

            var review = await _db.Reviews
                .Include(r => r.Booking)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (review == null) return NotFound();

            if (review.UserId != userId)
                return Forbid();

            if (review.Booking == null || review.Booking.Status != "Completed")
                return Forbid();

            ViewBag.BookingTitle = review.Booking.Title;
            ViewBag.BookingDateTime = review.Booking.dateTime;

            return View(review);
        }

        // EDIT (POST): only Patient, only review owner, and booking must be completed
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> Edit(int id, Review formModel)
        {
            if (id != formModel.Id) return NotFound();

            var userId = _userManager.GetUserId(User);

            var review = await _db.Reviews
                .Include(r => r.Booking)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (review == null) return NotFound();

            if (review.UserId != userId)
                return Forbid();

            if (review.Booking == null || review.Booking.Status != "Completed")
                return Forbid();

            if (!ModelState.IsValid)
            {
                ViewBag.BookingTitle = review.Booking.Title;
                ViewBag.BookingDateTime = review.Booking.dateTime;
                return View(review);
            }

            // only update editable fields
            review.Rating = formModel.Rating;
            review.Comment = formModel.Comment;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = review.Id });
        }
    }
}
