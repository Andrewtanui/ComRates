using System;
using System.Collections.Generic;

namespace TanuiApp.ViewModels
{
    public class DeliveryDashboardViewModel
    {
        public int TotalDeliveries { get; set; }
        public int PendingDeliveries { get; set; }
        public int InTransitDeliveries { get; set; }
        public int CompletedDeliveries { get; set; }
        public decimal TotalEarnings { get; set; }
        public decimal AverageDeliveryFee { get; set; }
        
        public List<DeliveryOrderInfo> PendingOrders { get; set; } = new List<DeliveryOrderInfo>();
        public List<DeliveryOrderInfo> ActiveDeliveries { get; set; } = new List<DeliveryOrderInfo>();
        public List<DeliveryOrderInfo> RecentDeliveries { get; set; } = new List<DeliveryOrderInfo>();
        public List<LocationStats> FrequentLocations { get; set; } = new List<LocationStats>();
        public List<DailyDeliveryStats> DeliveriesByDay { get; set; } = new List<DailyDeliveryStats>();
        public List<HourlyDeliveryStats> DeliveriesByHour { get; set; } = new List<HourlyDeliveryStats>();
    }

    public class DeliveryOrderInfo
    {
        public int OrderId { get; set; }
        public string TrackingNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
        public string DeliveryTown { get; set; } = string.Empty;
        public string DeliveryCounty { get; set; } = string.Empty;
        public string DeliveryStatus { get; set; } = string.Empty;
        public decimal DeliveryFee { get; set; }
        public decimal TotalAmount { get; set; }
        public int ItemCount { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class LocationStats
    {
        public string Location { get; set; } = string.Empty;
        public int DeliveryCount { get; set; }
        public decimal TotalEarnings { get; set; }
    }

    public class DailyDeliveryStats
    {
        public string DayName { get; set; } = string.Empty;
        public int DeliveryCount { get; set; }
        public decimal Earnings { get; set; }
    }

    public class HourlyDeliveryStats
    {
        public int Hour { get; set; }
        public string TimeRange { get; set; } = string.Empty;
        public int DeliveryCount { get; set; }
        public decimal Earnings { get; set; }
    }
}
