using Microsoft.AspNetCore.Identity;
using System;

namespace PsyConnect.Models
{
    public class EmailOTPCode
    {
        public int Id { get; set; }

        public string UserId { get; set; } = null!;
        public IdentityUser User { get; set; } = null!;

        public string Code { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; }                 // avoid reuse
    }
}
