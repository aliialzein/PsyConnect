using System;
using System.Collections.Generic;
using PsyConnect.Models;

namespace PsyConnect.Services
{
    public interface IBookingStatusService
    {
        void UpdateStatus(Booking booking);
        void UpdateStatus(IEnumerable<Booking> bookings);
    }

    public class BookingStatusService : IBookingStatusService
    {
        public void UpdateStatus(Booking booking)
        {
            if (booking == null) return;

            var now = DateTime.Now;
            var sessionDuration = TimeSpan.FromMinutes(50);

            if (booking.dateTime == default)
            {
                booking.Status = "Pending";
                return;
            }

            var start = booking.dateTime;

            if (start > now)
            {
                booking.Status = "Pending";
            }
            else if (start <= now && start.Add(sessionDuration) > now)
            {
                booking.Status = "InProgress";
            }
            else
            {
                booking.Status = "Completed";
            }
        }

        public void UpdateStatus(IEnumerable<Booking> bookings)
        {
            if (bookings == null) return;

            foreach (var booking in bookings)
            {
                UpdateStatus(booking);
            }
        }
    }
}
