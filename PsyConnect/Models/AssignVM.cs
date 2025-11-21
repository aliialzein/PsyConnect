using Microsoft.AspNetCore.Identity;

namespace PsyConnect.Models
{
    public class AssignVM
    {
        public int Id { get; set; }
        public IdentityUser User { get; set; }
        public IdentityRole Role { get; set; }
    }
}
