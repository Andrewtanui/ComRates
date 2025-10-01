using System.ComponentModel.DataAnnotations;
using TanuiApp.Models;

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

        // User Role Selection
        [Required(ErrorMessage = "Please select a user type")]
        [Display(Name = "User Type")]
        public UserRole UserRole { get; set; } = UserRole.Buyer;

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

        // Updated Kenyan location fields
        [Display(Name = "Street Address")]
        public string? Address { get; set; }

        [Display(Name = "Estate/Neighborhood")]
        public string? Estate { get; set; }

        [Display(Name = "Town/City")]
        public string? Town { get; set; }

        [Display(Name = "County")]
        public string? County { get; set; }

        [Display(Name = "Postal Code")]
        public string? PostalCode { get; set; }

        // Delivery Service specific fields
        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [Display(Name = "License Number")]
        public string? LicenseNumber { get; set; }

        [Display(Name = "Vehicle Information")]
        public string? VehicleInfo { get; set; }

        // Preferences
        [Display(Name = "Receive Email Notifications")]
        public bool EmailNotifications { get; set; } = true;

        [Display(Name = "Receive SMS Notifications")]
        public bool SmsNotifications { get; set; } = false;

        [Display(Name = "Make Profile Public")]
        public bool IsPublicProfile { get; set; } = true;
    }
}
