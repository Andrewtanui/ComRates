using System.ComponentModel.DataAnnotations;

namespace TanuiApp.ViewModels
{
    public class ProfileViewModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

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

        [StringLength(500)]
        [Display(Name = "Bio")]
        public string? Bio { get; set; }

        [Url]
        [Display(Name = "Profile Picture URL")]
        public string? ProfilePictureUrl { get; set; }

        [Display(Name = "Receive Email Notifications")]
        public bool EmailNotifications { get; set; }

        [Display(Name = "Receive SMS Notifications")]
        public bool SmsNotifications { get; set; }

        [Display(Name = "Make Profile Public")]
        public bool IsPublicProfile { get; set; }
    }
}