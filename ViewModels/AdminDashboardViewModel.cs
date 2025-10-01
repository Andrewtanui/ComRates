using System;
using System.Collections.Generic;
using TanuiApp.Models;

namespace TanuiApp.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalBuyers { get; set; }
        public int TotalSellers { get; set; }
        public int TotalDeliveryServices { get; set; }
        public int SuspendedAccounts { get; set; }
        public int BannedAccounts { get; set; }
        public int PendingReports { get; set; }
        
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int ActiveDeliveries { get; set; }
        
        public List<UserReportInfo> RecentReports { get; set; } = new List<UserReportInfo>();
        public List<UserInfo> RecentUsers { get; set; } = new List<UserInfo>();
        public List<UserInfo> FlaggedUsers { get; set; } = new List<UserInfo>();
        public List<DeliveryServiceInfo> PendingDeliveryServices { get; set; } = new List<DeliveryServiceInfo>();
        public List<DailyStats> UserGrowth { get; set; } = new List<DailyStats>();
        public List<OrderStats> OrdersByDay { get; set; } = new List<OrderStats>();
    }

    public class UserInfo
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsSuspended { get; set; }
        public bool IsBanned { get; set; }
        public int ReportCount { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }

    public class UserReportInfo
    {
        public int ReportId { get; set; }
        public string ReportedUserName { get; set; } = string.Empty;
        public string ReportedUserId { get; set; } = string.Empty;
        public string ReporterName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime ReportedAt { get; set; }
        public bool IsResolved { get; set; }
    }

    public class DeliveryServiceInfo
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string? LicenseNumber { get; set; }
        public string? VehicleInfo { get; set; }
        public string? Town { get; set; }
        public string? County { get; set; }
        public bool IsVerified { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class DailyStats
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }

    public class OrderStats
    {
        public string DayName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
    }
}
