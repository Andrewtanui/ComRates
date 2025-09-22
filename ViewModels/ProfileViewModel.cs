using System.ComponentModel.DataAnnotations;

namespace TanuiApp.ViewModels
{
    public class ProfileViewModel
    {
    [Required]
    [Display(Name = "Full Name")]
    public required string FullName { get; set; }

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public required string Email { get; set; }

    [Phone]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

    [Display(Name = "Address")]
    public string? Address { get; set; }

    [Display(Name = "City")]
    public string? City { get; set; }
        
        [Display(Name = "University")]
        public string? University { get; set; }
        
        [Display(Name = "University County")]
        public string? UniversityCounty { get; set; }

    [Display(Name = "State/Province")]
    public string? State { get; set; }

    [Display(Name = "Postal Code")]
    public string? PostalCode { get; set; }

    [Display(Name = "Country")]
    public string? Country { get; set; }

    [Display(Name = "Bio")]
    [StringLength(500, ErrorMessage = "Bio cannot exceed 500 characters.")]
    public string? Bio { get; set; }

    [Display(Name = "Profile Picture URL")]
    [Url(ErrorMessage = "Please enter a valid URL.")]
    public string? ProfilePictureUrl { get; set; }

    // Additional profile fields referenced by Settings.cshtml
    [Display(Name = "Is On Campus")]
    public bool IsOnCampus { get; set; }

    [Display(Name = "County")]
    public string? County { get; set; }

    [Display(Name = "Ward")]
    public string? Ward { get; set; }

    [Display(Name = "Estate")]
    public string? Estate { get; set; }

        // Account preferences
        [Display(Name = "Receive Email Notifications")]
        public bool EmailNotifications { get; set; } = true;

        [Display(Name = "Receive SMS Notifications")]
        public bool SmsNotifications { get; set; } = false;

        [Display(Name = "Make Profile Public")]
        public bool IsPublicProfile { get; set; } = true;
    }

    public class UpdatePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
    public required string CurrentPassword { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
    public required string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
    public required string ConfirmPassword { get; set; }
    }
}