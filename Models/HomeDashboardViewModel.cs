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
    }
}
