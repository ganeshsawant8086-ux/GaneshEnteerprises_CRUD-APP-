using System.Diagnostics;
using GaneshEnterprises.Data;
using GaneshEnterprises.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GaneshEnterprises.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;
        private readonly EnterpriseInfo _enterpriseInfo;

        public HomeController(
            ApplicationDbContext context, 
            ILogger<HomeController> logger,
            IOptions<EnterpriseInfo> enterpriseOptions)
        {
            _context = context;
            _logger = logger;
            _enterpriseInfo = enterpriseOptions.Value;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var totalProducts = await _context.Products.CountAsync();
                var totalStock = await _context.Products.SumAsync(p => (long)p.StockQuantity);
                var totalOrders = await _context.Orders.CountAsync();
                var pendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending");

                var smallBagsCount = await _context.Products.CountAsync(p => p.BagSize == BagSize.Small);
                var mediumBagsCount = await _context.Products.CountAsync(p => p.BagSize == BagSize.Medium);
                var largeBagsCount = await _context.Products.CountAsync(p => p.BagSize == BagSize.Large);

                var featuredProducts = await _context.Products
                    .Where(p => p.IsAvailable)
                    .OrderBy(p => p.BagSize)
                    .Take(6)
                    .ToListAsync();

                // Only query recent orders if user is Admin to prevent data exposure
                var isAdmin = User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Admin");
                var recentOrders = isAdmin 
                    ? await _context.Orders.Include(o => o.Product).OrderByDescending(o => o.Id).Take(5).ToListAsync()
                    : new List<Order>();

                var viewModel = new HomeDashboardViewModel
                {
                    TotalProducts = totalProducts,
                    TotalStockUnits = (int)Math.Min(totalStock, int.MaxValue),
                    TotalOrders = totalOrders,
                    PendingOrders = pendingOrders,
                    SmallBagsCount = smallBagsCount,
                    MediumBagsCount = mediumBagsCount,
                    LargeBagsCount = largeBagsCount,
                    FeaturedProducts = featuredProducts,
                    RecentOrders = recentOrders
                };

                ViewBag.EnterpriseInfo = _enterpriseInfo;
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading home page dashboard data.");
                ViewBag.ErrorMessage = "Unable to connect to the SQL Server database. Please verify the connection in appsettings.json.";
                return View(new HomeDashboardViewModel());
            }
        }

        // ==========================================
        // TRACKING: Redirect to protected customer or admin portal
        // Public unauthenticated tracking search is disabled for privacy
        // ==========================================
        [HttpGet]
        public IActionResult Tracking()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Tracking", "Admin");
                }
                if (User.IsInRole("Customer"))
                {
                    return RedirectToAction("MyOrders", "Orders");
                }
            }

            return RedirectToAction("CustomerLogin", "Account");
        }

        public IActionResult About()
        {
            ViewBag.EnterpriseInfo = _enterpriseInfo;
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
