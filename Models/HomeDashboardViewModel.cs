namespace GaneshEnterprises.Models
{
    public class HomeDashboardViewModel
    {
        public int TotalProducts { get; set; }
        public int TotalStockUnits { get; set; }
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }

        public int SmallBagsCount { get; set; }
        public int MediumBagsCount { get; set; }
        public int LargeBagsCount { get; set; }

        public List<Product> FeaturedProducts { get; set; } = new();
        public List<Order> RecentOrders { get; set; } = new();

        // Total Benefit (Delivered / Completed Orders) Analytics for Admin
        public decimal DeliveredBenefitToday { get; set; }
        public decimal DeliveredBenefitMonth { get; set; }
        public decimal DeliveredBenefitYear { get; set; }
        public decimal DeliveredBenefitAllTime { get; set; }

        public int DeliveredCountToday { get; set; }
        public int DeliveredCountMonth { get; set; }
        public int DeliveredCountYear { get; set; }
        public int DeliveredCountAllTime { get; set; }

        // Warehouse Stock Health Analytics (Down Stock / Red vs High Stock / Green)
        public int LowStockCount { get; set; }
        public int MidStockCount { get; set; }
        public int HighStockCount { get; set; }
    }
}
