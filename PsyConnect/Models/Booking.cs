using Microsoft.AspNetCore.Identity;

namespace PsyConnect.Models
{
    public class Booking
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int Number { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public DateTime dateTime { get; set; }

        public string UserId { get; set; }
        public IdentityUser User { get; set; }
    }
}
