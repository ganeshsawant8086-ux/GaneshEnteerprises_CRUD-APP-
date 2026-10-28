using System.ComponentModel.DataAnnotations;

namespace GaneshEnterprises.Models
{
    public class Retailer
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Business or Store Name is required.")]
        [StringLength(150, MinimumLength = 2)]
        [Display(Name = "Business / Store Name")]
        public string BusinessName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Owner or Contact Person Name is required.")]
        [StringLength(100)]
        [Display(Name = "Owner Name")]
        public string OwnerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required for login.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(120)]
        [Display(Name = "Email Address (Login ID)")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone]
        [StringLength(20)]
        [Display(Name = "Phone / Mobile Number")]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Password")]
        public string Password { get; set; } = "Password@123";

        [StringLength(250)]
        [Display(Name = "Store Address")]
        public string? Address { get; set; }

        [StringLength(100)]
        [Display(Name = "City")]
        public string? City { get; set; }

        [StringLength(100)]
        [Display(Name = "State")]
        public string? State { get; set; }

        [StringLength(20)]
        [Display(Name = "Pincode")]
        public string? Pincode { get; set; }

        [StringLength(50)]
        [Display(Name = "GSTIN / Trade License")]
        public string? GstNumber { get; set; }

        [Display(Name = "Account Status")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Registration Date")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation for orders placed by this retailer
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
