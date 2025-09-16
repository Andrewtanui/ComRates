using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TanuiApp.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Product name is required.")]
        [StringLength(100, ErrorMessage = "Product name cannot exceed 100 characters.")]
        [Display(Name = "Product Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Price")]
        public decimal Price { get; set; }

        [Display(Name = "Image")]
        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters.")]
        [Display(Name = "Category")]
        public string Category { get; set; } = string.Empty;

        [Display(Name = "On Sale")]
        public bool OnSale { get; set; } = false;

        [Display(Name = "Sale End Date")]
        public DateTime? SaleEndDate { get; set; }

        [Range(0, 5, ErrorMessage = "Rating must be between 0 and 5.")]
        public double Rating { get; set; } = 0;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Display(Name = "Created Date")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

        [ForeignKey("UserId")]
        public virtual Users? User { get; set; }
    }
}