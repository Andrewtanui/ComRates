using System.ComponentModel.DataAnnotations;

namespace TanuiApp.ViewModels
{
    public class VerifyOtpViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "OTP is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits")]
        [Display(Name = "Enter OTP")]
        public string OTP { get; set; }
    }
}
