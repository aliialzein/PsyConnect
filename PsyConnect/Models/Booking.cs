using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace PsyConnect.Models
{
    public class Booking
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        // Will be set automatically per patient (1,2,3,...)
        [Range(1, int.MaxValue)]
        public int Number { get; set; }

        // Only "Onsite" or "Online"
        [Required]
        [RegularExpression("Onsite|Online", ErrorMessage = "Type must be Onsite or Online.")]
        public string Type { get; set; }
        public string? MeetingLink { get; set; }

        // Status controlled by system, not user
        [Required]
        public string Status { get; set; } = "Pending";

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime dateTime { get; set; }

        [Required]
        public string UserId { get; set; }

        public IdentityUser User { get; set; }
        public bool PatientReminderSent { get; set; }
    }
}
