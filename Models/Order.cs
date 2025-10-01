using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace TanuiApp.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Payment status: Paid, Pending
        
        // Delivery Information
        public string? DeliveryServiceId { get; set; }
        public string DeliveryStatus { get; set; } = "Preparing"; // Preparing, Packed, InTransit, Delivered
        public string? DeliveryAddress { get; set; }
        public string? DeliveryTown { get; set; }
        public string? DeliveryCounty { get; set; }
        public decimal DeliveryFee { get; set; } = 0;
        public DateTime? DeliveryDate { get; set; }
        public string? DeliveryNotes { get; set; }
        
        // Tracking
        public string? TrackingNumber { get; set; }
        public DateTime? PackedAt { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        
        // Buyer location coordinates (for map tracking)
        public double? BuyerLatitude { get; set; }
        public double? BuyerLongitude { get; set; }
        
        // Navigation properties
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
        
        [ForeignKey("DeliveryServiceId")]
        public virtual Users? DeliveryService { get; set; }
        
        [ForeignKey("UserId")]
        public virtual Users? Buyer { get; set; }
    }
}
