using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PsyConnect.Data;
using PsyConnect.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using PsyConnect.Services;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IBookingStatusService _bookingStatusService;

    public HomeController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IBookingStatusService bookingStatusService)
    {
        _context = context;
        _userManager = userManager;
        _bookingStatusService = bookingStatusService;
    }

    // ====================== HOME ROUTING ======================
    [AllowAnonymous]
    public IActionResult Index()
    {
        // Redirect logged users to dashboard
        if (User.IsInRole("Admin"))
            return RedirectToAction(nameof(AdminDashboard));

        if (User.IsInRole("Patient"))
            return RedirectToAction(nameof(PatientDashboard));

        // Not logged in
        return View();
    }

    // ====================== ADMIN DASHBOARD ======================
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminDashboard()
    {
        var now = DateTime.Now;
        var startToday = now.Date;
        var endWeek = startToday.AddDays(7);

        var bookingsQuery = _context.Bookings.Include(b => b.User);

        _bookingStatusService.UpdateStatus(bookingsQuery);
        await _context.SaveChangesAsync();

        // KPIs
        ViewBag.TotalBookings = await bookingsQuery.CountAsync();
        ViewBag.TodayBookings = await bookingsQuery.CountAsync(b => b.dateTime.Date == startToday);
        ViewBag.Next7DaysBookings = await bookingsQuery.CountAsync(b => b.dateTime.Date >= startToday &&
                                                                         b.dateTime.Date < endWeek);
        ViewBag.PendingCount = await bookingsQuery.CountAsync(b => b.Status == "Pending");
        ViewBag.InProgressCount = await bookingsQuery.CountAsync(b => b.Status == "InProgress");
        ViewBag.CompletedCount = await bookingsQuery.CountAsync(b => b.Status == "Completed");

        // Upcoming
        var upcoming = await bookingsQuery
            .Where(b => b.dateTime >= startToday)
            .OrderBy(b => b.dateTime)
            .Take(20)
            .ToListAsync();

        // Weekly chart
        var weeklyLabels = new string[7];
        var weeklyValues = new int[7];

        for (int i = 0; i < 7; i++)
        {
            var day = startToday.AddDays(i);
            weeklyLabels[i] = day.ToString("dd MMM");
            weeklyValues[i] = await bookingsQuery.CountAsync(b => b.dateTime.Date == day);
        }

        ViewBag.WeeklyLabels = weeklyLabels;
        ViewBag.WeeklyValues = weeklyValues;

        // Online vs Onsite
        ViewBag.OnsiteCount = await bookingsQuery.CountAsync(b => b.Type == "Onsite");
        ViewBag.OnlineCount = await bookingsQuery.CountAsync(b => b.Type == "Online");

        return View("AdminDashboard", upcoming);
    }

    // ====================== PATIENT DASHBOARD ======================
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> PatientDashboard()
    {
        var userId = _userManager.GetUserId(User);
        var today = DateTime.Today;

        var myBookings = _context.Bookings.Where(b => b.UserId == userId);
        _bookingStatusService.UpdateStatus(myBookings);
        await _context.SaveChangesAsync();

        ViewBag.TotalMyBookings = await myBookings.CountAsync();
        ViewBag.UpcomingMyBookings = await myBookings.CountAsync(b => b.dateTime >= today);
        ViewBag.CompletedMyBookings = await myBookings.CountAsync(b => b.Status == "Completed");

        var upcoming = await myBookings
            .Where(b => b.dateTime >= today)
            .OrderBy(b => b.dateTime)
            .Take(20)
            .ToListAsync();

        ViewBag.NextSession = upcoming.FirstOrDefault();

        // Monthly chart
        var labels = new string[6];
        var values = new int[6];

        var start = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-5);

        for (int i = 0; i < 6; i++)
        {
            var mStart = start.AddMonths(i);
            var mEnd = mStart.AddMonths(1);

            labels[i] = mStart.ToString("MMM yy");
            values[i] = await myBookings.CountAsync(b =>
                b.dateTime >= mStart && b.dateTime < mEnd);
        }

        ViewBag.MonthlyLabels = labels;
        ViewBag.MonthlyValues = values;

        return View("PatientDashboard", upcoming);
    }

    // ====================== PRIVACY ======================
    public IActionResult Privacy()
    {
        return View();
    }
}
