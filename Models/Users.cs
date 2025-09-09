using Microsoft.AspNetCore.Identity;

namespace TanuiApp.Models
{
    public class Users : IdentityUser
    {
        public required String FullName { get; set; }
        public string? LastLoginAt { get; internal set; }
        public DateTime CreatedAt { get; internal set; }
        public DateTime? DateOfBirth { get; internal set; }
        public string? Address { get; set; }  // allow null
        public string? City { get; internal set; }
        public string? PostalCode { get; internal set; }
        public string? State { get; internal set; }
        public string? Country { get; internal set; }
        public bool SmsNotifications { get; internal set; }
        public bool EmailNotifications { get; internal set; }
        public string? Bio { get; internal set; }
        public string? ProfilePictureUrl { get; internal set; }
        public bool IsPublicProfile { get; internal set; }
    }
}
