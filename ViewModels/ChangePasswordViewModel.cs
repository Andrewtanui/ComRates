using System.ComponentModel.DataAnnotations;

namespace TanuiApp.ViewModels
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Email is Required")]
        [EmailAddress]
        public string Email { get; set; }


        [Required(ErrorMessage = "New Password is Required")]
        [StringLength(40, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        [Compare("ConfirmNewPassword", ErrorMessage = "The password and confirmation password do not match.")]
        public string NewPassword { get; set; }


        [Required(ErrorMessage = "Confirm New Password is Required")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        public string ConfirmNewPassword { get; set; }
    }
}
