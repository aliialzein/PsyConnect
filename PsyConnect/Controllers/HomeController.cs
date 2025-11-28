using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PsyConnect.Data;
using PsyConnect.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public HomeController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // ====================== DASHBOARD ROUTING ======================
    [AllowAnonymous]

    public async Task<IActionResult> Index()
    {
        if (User.IsInRole("Admin"))
        {
            return await AdminDashboard();
        }

        if (User.IsInRole("Patient"))
        {
            return await PatientDashboard();
        }

        // fallback: normal Home/Index view
        return View();
    }

    // ====================== ADMIN DASHBOARD ======================
    [Authorize]

    private async Task<IActionResult> AdminDashboard()
    {
        var now = DateTime.Now;
        var startToday = now.Date;
        var endWeek = startToday.AddDays(7);

        var bookingsQuery = _context.Bookings.Include(b => b.User);

        // summary numbers
        ViewBag.TotalBookings = await bookingsQuery.CountAsync();
        ViewBag.TodayBookings = await bookingsQuery.CountAsync(b => b.dateTime.Date == startToday);
        ViewBag.Next7DaysBookings = await bookingsQuery.CountAsync(b => b.dateTime.Date >= startToday &&
                                                                         b.dateTime.Date < endWeek);
        ViewBag.PendingCount = await bookingsQuery.CountAsync(b => b.Status == "Pending");
        ViewBag.InProgressCount = await bookingsQuery.CountAsync(b => b.Status == "InProgress");
        ViewBag.CompletedCount = await bookingsQuery.CountAsync(b => b.Status == "Completed");

        // upcoming list for "calendar" section
        var upcoming = await bookingsQuery
            .Where(b => b.dateTime >= startToday)
            .OrderBy(b => b.dateTime)
            .Take(20)
            .ToListAsync();

        // chart: bookings per day next 7 days
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

        // chart: onsite vs online
        ViewBag.OnsiteCount = await bookingsQuery.CountAsync(b => b.Type == "Onsite");
        ViewBag.OnlineCount = await bookingsQuery.CountAsync(b => b.Type == "Online");

        // model = list of upcoming bookings
        return View("AdminDashboard", upcoming);
    }

    // ====================== PATIENT DASHBOARD ======================
    [Authorize]
    private async Task<IActionResult> PatientDashboard()
    {
        var userId = _userManager.GetUserId(User);
        var now = DateTime.Now;
        var startToday = now.Date;

        var myBookings = _context.Bookings.Where(b => b.UserId == userId);

        ViewBag.TotalMyBookings = await myBookings.CountAsync();
        ViewBag.UpcomingMyBookings = await myBookings.CountAsync(b => b.dateTime >= startToday);
        ViewBag.CompletedMyBookings = await myBookings.CountAsync(b => b.Status == "Completed");

        var upcomingMyList = await myBookings
            .Where(b => b.dateTime >= startToday)
            .OrderBy(b => b.dateTime)
            .Take(20)
            .ToListAsync();

        ViewBag.NextSession = upcomingMyList.FirstOrDefault();

        // chart: last 6 months per month
        var monthlyLabels = new string[6];
        var monthlyValues = new int[6];

        var start = new DateTime(now.Year, now.Month, 1).AddMonths(-5);
        for (int i = 0; i < 6; i++)
        {
            var monthStart = start.AddMonths(i);
            var monthEnd = monthStart.AddMonths(1);

            monthlyLabels[i] = monthStart.ToString("MMM yy");
            monthlyValues[i] = await myBookings.CountAsync(b =>
                b.dateTime >= monthStart && b.dateTime < monthEnd);
        }

        ViewBag.MonthlyLabels = monthlyLabels;
        ViewBag.MonthlyValues = monthlyValues;

        return View("PatientDashboard", upcomingMyList);
    }
}