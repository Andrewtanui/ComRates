using System.ComponentModel.DataAnnotations;

namespace TanuiApp.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Name is Required")]
        public string Name { get; set; }


        [Required(ErrorMessage = "Email is Required")]
        [EmailAddress]
        public string Email { get; set; }


        [Required(ErrorMessage = "Password is Required")]
        [StringLength(40, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        [Compare("ConfirmPassword", ErrorMessage = "The password and confirmation password do not match.")]
        public string Password { get; set; }


        [Required(ErrorMessage = "Confirm Password is Required")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; }

        // Onboarding profile details
        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [Url]
        [Display(Name = "Profile Picture URL")]
        public string? ProfilePictureUrl { get; set; }

        [StringLength(500)]
        [Display(Name = "Bio")]
        public string? Bio { get; set; }

        [Display(Name = "Address")]
        public string? Address { get; set; }

        [Display(Name = "City")]
        public string? City { get; set; }

        [Display(Name = "State/Province")]
        public string? State { get; set; }

        [Display(Name = "Postal Code")]
        public string? PostalCode { get; set; }

        [Display(Name = "Country")]
        public string? Country { get; set; }

        // Preferences
        [Display(Name = "Receive Email Notifications")]
        public bool EmailNotifications { get; set; } = true;

        [Display(Name = "Receive SMS Notifications")]
        public bool SmsNotifications { get; set; } = false;

        [Display(Name = "Make Profile Public")]
        public bool IsPublicProfile { get; set; } = true;
    }
}