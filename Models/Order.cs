using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GaneshEnterprises.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [StringLength(50)]
        [Display(Name = "Order Reference #")]
        public string? OrderNumber { get; set; }

        [Required(ErrorMessage = "Customer or Business Name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
        [Display(Name = "Customer / Business Name")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact number is required.")]
        [RegularExpression(@"^[0-9\-\+\s\(\)]{7,20}$", ErrorMessage = "Please enter a valid phone number (7 to 20 digits).")]
        [Display(Name = "Contact Number")]
        public string ContactNumber { get; set; } = string.Empty;

        [Display(Name = "Order Type")]
        public string OrderType { get; set; } = "Customer"; // "Customer" or "Retailer"

        // Optional Retailer linkage
        [Display(Name = "Linked Retailer Account")]
        public int? RetailerId { get; set; }

        [ForeignKey("RetailerId")]
        public Retailer? Retailer { get; set; }

        [Required(ErrorMessage = "Please select a plastic bag product.")]
        [Display(Name = "Plastic Bag Product")]
        public int ProductId { get; set; }

        // Navigation property
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        [Required(ErrorMessage = "Order quantity is required.")]
        [Range(1, 1000000, ErrorMessage = "Quantity must be at least 1 unit.")]
        [Display(Name = "Quantity (Pcs)")]
        public int Quantity { get; set; } = 500;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Unit Price (₹)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Total Amount (₹)")]
        public decimal TotalAmount { get; set; }

        [Required(ErrorMessage = "Order date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Order Date")]
        public DateTime OrderDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Order status is required.")]
        [StringLength(50)]
        [Display(Name = "Order Status")]
        public string Status { get; set; } = "Pending"; // "Pending", "Processing", "Shipped", "Delivered", "Cancelled"

        // Tracking history records
        public ICollection<OrderTracking> TrackingUpdates { get; set; } = new List<OrderTracking>();

        // Multi-product Cart Order Items
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
