using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace PsyConnect.Models
{
    public class Payment
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }
        public IdentityUser User { get; set; }

        [Required]
        [Range(0.0, double.MaxValue)]
        public decimal Amount { get; set; }

        // "Pending", "Paid", "Failed", "Canceled"
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Booking snapshot
        [Required]
        [StringLength(100)]
        public string BookingTitle { get; set; }

        [StringLength(500)]
        public string? BookingDescription { get; set; }

        [Required]
        [RegularExpression("Onsite|Online", ErrorMessage = "Type must be Onsite or Online.")]
        public string BookingType { get; set; }

        [Required]
        public DateTime BookingDateTime { get; set; }
    }
}
