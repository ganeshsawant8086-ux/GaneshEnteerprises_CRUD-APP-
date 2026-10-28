using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GaneshEnterprises.Models
{
    public class OrderTracking
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Associated Order")]
        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public Order? Order { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Tracking / Consignment #")]
        public string TrackingNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tracking status is required.")]
        [StringLength(100)]
        [Display(Name = "Current Milestone")]
        public string CurrentStatus { get; set; } = "Order Packed & Dispatched";

        [Required(ErrorMessage = "Current location is required.")]
        [StringLength(150)]
        [Display(Name = "Current Location / Hub")]
        public string CurrentLocation { get; set; } = "Ganesh Enterprises Central Hub";

        [Required(ErrorMessage = "Carrier name is required.")]
        [StringLength(100)]
        [Display(Name = "Logistics Carrier")]
        public string CarrierName { get; set; } = "Ganesh Express Logistics";

        [DataType(DataType.Date)]
        [Display(Name = "Estimated Delivery Date")]
        public DateTime? EstimatedDelivery { get; set; }

        [Display(Name = "Last Updated")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [StringLength(500)]
        [Display(Name = "Logistics Remarks / Notes")]
        public string? Notes { get; set; }
    }
}
