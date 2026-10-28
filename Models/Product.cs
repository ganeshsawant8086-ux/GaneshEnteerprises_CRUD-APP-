using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GaneshEnterprises.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Product Name is required.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Product Name must be between 3 and 100 characters.")]
        [Display(Name = "Product Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "Description must be between 5 and 500 characters.")]
        [Display(Name = "Bag Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a Bag Size.")]
        [Display(Name = "Bag Size")]
        public BagSize BagSize { get; set; } = BagSize.Medium;

        [Required(ErrorMessage = "Customer Price is required.")]
        [Range(0.01, 100000.00, ErrorMessage = "Customer Price must be greater than 0.")]
        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        [Display(Name = "Customer Price / MRP (₹)")]
        public decimal CustomerPrice { get; set; }

        [Required(ErrorMessage = "Retailer Price is required.")]
        [Range(0.01, 100000.00, ErrorMessage = "Retailer Price must be greater than 0.")]
        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        [Display(Name = "Wholesale Retailer Price (₹)")]
        public decimal RetailerPrice { get; set; }

        // Backward compatibility property
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Base Price (₹)")]
        public decimal Price
        {
            get => CustomerPrice;
            set => CustomerPrice = value;
        }

        [Required(ErrorMessage = "Stock quantity is required.")]
        [Range(0, 1000000, ErrorMessage = "Stock quantity must be between 0 and 1,000,000 units.")]
        [Display(Name = "Stock Quantity (Units)")]
        public int StockQuantity { get; set; }

        [Display(Name = "Is Available for Orders")]
        public bool IsAvailable { get; set; } = true;

        [NotMapped]
        [Display(Name = "Category")]
        public string CategoryName => BagSize switch
        {
            BagSize.Small => "Small Bags",
            BagSize.Medium => "Medium Bags",
            BagSize.Large => "Large Bags",
            _ => "Plastic Bags"
        };

        // Computed Retailer Selling Margin Helper
        [NotMapped]
        [Display(Name = "Retailer Margin (₹/Unit)")]
        public decimal Margin => CustomerPrice > RetailerPrice ? CustomerPrice - RetailerPrice : 0;

        [NotMapped]
        [Display(Name = "Profit Margin (%)")]
        public decimal MarginPercentage => CustomerPrice > 0 ? (Margin / CustomerPrice) * 100 : 0;

        // Navigation property for related customer and retailer orders
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
