using Microsoft.AspNetCore.Identity;

namespace TanuiApp.Models
{
    public enum UserRole
    {
        Buyer,
        Seller,
        DeliveryService,
        SystemAdmin
    }

    public class Users : IdentityUser
    {
        public required String FullName { get; set; }
        public string? LastLoginAt { get; internal set; }
        public DateTime CreatedAt { get; internal set; }
        public DateTime? DateOfBirth { get; internal set; }
        
        // Updated Kenyan location fields
        public string? Address { get; set; }  // Street address
        public string? Estate { get; set; }   // Estate/Neighborhood
        public string? Town { get; set; }     // Town/City
        public string? County { get; set; }   // County (replaces State)
        public string? PostalCode { get; set; }
        
        public bool SmsNotifications { get; internal set; }
        public bool EmailNotifications { get; internal set; }
        public string? Bio { get; internal set; }
        public string? ProfilePictureUrl { get; internal set; }
        public bool IsPublicProfile { get; internal set; }

        // Role-based user system
        public UserRole UserRole { get; set; } = UserRole.Buyer;

        // Delivery service specific fields
        public string? CompanyName { get; set; }
        public string? LicenseNumber { get; set; }
        public string? VehicleInfo { get; set; }
        public bool IsVerified { get; set; } = false;
        public decimal? DeliveryRating { get; set; }

        // Account moderation fields
        public bool IsSuspended { get; set; } = false;
        public bool IsBanned { get; set; } = false;
        public DateTime? SuspendedAt { get; set; }
        public DateTime? BannedAt { get; set; }
        public string? SuspensionReason { get; set; }
        public string? BanReason { get; set; }
        public int ReportCount { get; set; } = 0;
        public DateTime? LastReportedAt { get; set; }
    }
}