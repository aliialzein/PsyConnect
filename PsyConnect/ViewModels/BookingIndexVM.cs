using System.Collections.Generic;
using PsyConnect.Models;

namespace PsyConnect.ViewModels
{
    public class BookingIndexVM
    {
        public IEnumerable<Booking> Bookings { get; set; } = new List<Booking>();

        public int PageNumber { get; set; }
        public int TotalPages { get; set; }

        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
