using System;
using System.Collections.Generic;

namespace TanuiApp.ViewModels
{
    public class SellerDashboardViewModel
    {
        public int TotalProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        
        public List<ProductPerformance> TopSellingProducts { get; set; } = new List<ProductPerformance>();
        public List<ProductPerformance> LowPerformingProducts { get; set; } = new List<ProductPerformance>();
        public List<DailySalesData> SalesByDay { get; set; } = new List<DailySalesData>();
        public List<HourlySalesData> SalesByHour { get; set; } = new List<HourlySalesData>();
        public List<RecentOrder> RecentOrders { get; set; } = new List<RecentOrder>();
        public List<ProductStockAlert> StockAlerts { get; set; } = new List<ProductStockAlert>();
    }

    public class ProductPerformance
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int UnitsSold { get; set; }
        public decimal Revenue { get; set; }
        public int CurrentStock { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class DailySalesData
    {
        public string DayName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class HourlySalesData
    {
        public int Hour { get; set; }
        public string TimeRange { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class RecentOrder
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ItemCount { get; set; }
    }

    public class ProductStockAlert
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public string AlertLevel { get; set; } = string.Empty; // "Low" or "Out"
        public string? ImageUrl { get; set; }
    }
}
