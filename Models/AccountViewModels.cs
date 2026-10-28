using System.ComponentModel.DataAnnotations;

namespace GaneshEnterprises.Models
{
    public class AdminLoginViewModel
    {
        [Required(ErrorMessage = "Admin Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        [Display(Name = "Admin Email Address")]
        public string Email { get; set; } = "ganeshenterprises@gmail.com";

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Admin Password")]
        public string Password { get; set; } = "Admin@123";

        [Display(Name = "Remember Me")]
        public bool RememberMe { get; set; } = true;
    }

    public class RetailerLoginViewModel
    {
        [Required(ErrorMessage = "Registered Email or Phone is required.")]
        [Display(Name = "Registered Email or Mobile #")]
        public string EmailOrPhone { get; set; } = "om.retail@gmail.com";

        [DataType(DataType.Password)]
        [Display(Name = "Account Password")]
        public string? Password { get; set; } = "Password@123";

        [Display(Name = "Enter 6-Digit SMS OTP")]
        [StringLength(6, MinimumLength = 4, ErrorMessage = "OTP must be 4 to 6 digits.")]
        public string? OtpCode { get; set; }

        public string LoginMode { get; set; } = "password"; // "password" or "otp"
    }

    public class RetailerPortalViewModel
    {
        public Retailer CurrentRetailer { get; set; } = null!;
        public List<Product> Products { get; set; } = new();
        public List<Order> RetailerOrders { get; set; } = new();
    }

    public class OrderTrackingViewModel
    {
        [Display(Name = "Search Order # or Tracking Code")]
        public string? SearchQuery { get; set; }
        public Order? SelectedOrder { get; set; }
        public List<OrderTracking> TrackingMilestones { get; set; } = new();
    }
}
