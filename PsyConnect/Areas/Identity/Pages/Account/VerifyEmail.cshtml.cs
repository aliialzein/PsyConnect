using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PsyConnect.Services;

namespace PsyConnect.Areas.Identity.Pages.Account
{
    public class VerifyEmailModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IEmailOTPService _emailOtpService;

        public VerifyEmailModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IEmailOTPService emailOtpService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailOtpService = emailOtpService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            public string UserId { get; set; }

            [Required, EmailAddress]
            public string Email { get; set; }

            [Required]
            [Display(Name = "Verification Code")]
            public string Code { get; set; }
        }

        public IActionResult OnGet(string userId, string email)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email))
            {
                return RedirectToPage("Login");
            }

            Input = new InputModel
            {
                UserId = userId,
                Email = email
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await _userManager.FindByIdAsync(Input.UserId);
            if (user == null || user.Email != Input.Email)
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                return Page();
            }

            var valid = await _emailOtpService.VerifyOtpAsync(user, Input.Code);
            if (!valid)
            {
                ModelState.AddModelError(string.Empty, "Invalid or expired code.");
                return Page();
            }

            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);

            await _signInManager.SignInAsync(user, isPersistent: false);

            return LocalRedirect("~/");
        }
    }
}
