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

                // Stock Health Statistics (Down Stock / Low = Red, High Stock = Green)
                var lowStockCount = await _context.Products.CountAsync(p => p.StockQuantity < 12000);
                var midStockCount = await _context.Products.CountAsync(p => p.StockQuantity >= 12000 && p.StockQuantity <= 25000);
                var highStockCount = await _context.Products.CountAsync(p => p.StockQuantity > 25000);

                var featuredProducts = await _context.Products
                    .Where(p => p.IsAvailable)
                    .OrderBy(p => p.BagSize)
                    .Take(6)
                    .ToListAsync();

                // Only query recent orders and delivered benefit metrics if user is Admin
                var isAdmin = User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Admin");
                var recentOrders = isAdmin 
                    ? await _context.Orders.Include(o => o.Product).OrderByDescending(o => o.Id).Take(5).ToListAsync()
                    : new List<Order>();

                decimal deliveredToday = 0;
                decimal deliveredMonth = 0;
                decimal deliveredYear = 0;
                decimal deliveredAllTime = 0;
                int countToday = 0;
                int countMonth = 0;
                int countYear = 0;
                int countAllTime = 0;

                if (isAdmin)
                {
                    var today = DateTime.Today;
                    var tomorrow = today.AddDays(1);
                    var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
                    var firstDayOfNextMonth = firstDayOfMonth.AddMonths(1);
                    var firstDayOfYear = new DateTime(today.Year, 1, 1);
                    var firstDayOfNextYear = firstDayOfYear.AddYears(1);

                    var deliveredOrders = await _context.Orders
                        .Where(o => o.Status == "Delivered" || o.Status == "Completed")
                        .Select(o => new { o.TotalAmount, o.OrderDate })
                        .ToListAsync();

                    deliveredToday = deliveredOrders.Where(o => o.OrderDate >= today && o.OrderDate < tomorrow).Sum(o => o.TotalAmount);
                    countToday = deliveredOrders.Count(o => o.OrderDate >= today && o.OrderDate < tomorrow);

                    deliveredMonth = deliveredOrders.Where(o => o.OrderDate >= firstDayOfMonth && o.OrderDate < firstDayOfNextMonth).Sum(o => o.TotalAmount);
                    countMonth = deliveredOrders.Count(o => o.OrderDate >= firstDayOfMonth && o.OrderDate < firstDayOfNextMonth);

                    deliveredYear = deliveredOrders.Where(o => o.OrderDate >= firstDayOfYear && o.OrderDate < firstDayOfNextYear).Sum(o => o.TotalAmount);
                    countYear = deliveredOrders.Count(o => o.OrderDate >= firstDayOfYear && o.OrderDate < firstDayOfNextYear);

                    deliveredAllTime = deliveredOrders.Sum(o => o.TotalAmount);
                    countAllTime = deliveredOrders.Count;
                }

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
                    RecentOrders = recentOrders,
                    DeliveredBenefitToday = deliveredToday,
                    DeliveredBenefitMonth = deliveredMonth,
                    DeliveredBenefitYear = deliveredYear,
                    DeliveredBenefitAllTime = deliveredAllTime,
                    DeliveredCountToday = countToday,
                    DeliveredCountMonth = countMonth,
                    DeliveredCountYear = countYear,
                    DeliveredCountAllTime = countAllTime,
                    LowStockCount = lowStockCount,
                    MidStockCount = midStockCount,
                    HighStockCount = highStockCount
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
        // API: REAL-TIME DELIVERED BENEFIT STATS (AUTO MODE)
        // Day, Monthly, Year, and All-Time calculations
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetDeliveredBenefitStats()
        {
            var isAdmin = User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Admin");
            if (!isAdmin)
            {
                return Unauthorized(new { success = false, message = "Admin authorization required." });
            }

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
            var firstDayOfNextMonth = firstDayOfMonth.AddMonths(1);
            var firstDayOfYear = new DateTime(today.Year, 1, 1);
            var firstDayOfNextYear = firstDayOfYear.AddYears(1);

            var deliveredOrders = await _context.Orders
                .Where(o => o.Status == "Delivered" || o.Status == "Completed")
                .Include(o => o.Product)
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.Id)
                .ToListAsync();

            var todayOrders = deliveredOrders.Where(o => o.OrderDate >= today && o.OrderDate < tomorrow).ToList();
            var monthOrders = deliveredOrders.Where(o => o.OrderDate >= firstDayOfMonth && o.OrderDate < firstDayOfNextMonth).ToList();
            var yearOrders = deliveredOrders.Where(o => o.OrderDate >= firstDayOfYear && o.OrderDate < firstDayOfNextYear).ToList();

            var todayTotal = todayOrders.Sum(o => o.TotalAmount);
            var monthTotal = monthOrders.Sum(o => o.TotalAmount);
            var yearTotal = yearOrders.Sum(o => o.TotalAmount);
            var allTimeTotal = deliveredOrders.Sum(o => o.TotalAmount);

            return Json(new
            {
                success = true,
                lastUpdated = DateTime.Now.ToString("hh:mm:ss tt"),
                today = new
                {
                    amount = todayTotal,
                    formatted = "₹" + todayTotal.ToString("N0"),
                    count = todayOrders.Count
                },
                month = new
                {
                    amount = monthTotal,
                    formatted = "₹" + monthTotal.ToString("N0"),
                    count = monthOrders.Count
                },
                year = new
                {
                    amount = yearTotal,
                    formatted = "₹" + yearTotal.ToString("N0"),
                    count = yearOrders.Count
                },
                allTime = new
                {
                    amount = allTimeTotal,
                    formatted = "₹" + allTimeTotal.ToString("N0"),
                    count = deliveredOrders.Count
                },
                recentOrders = deliveredOrders.Take(8).Select(o => new
                {
                    id = o.Id,
                    orderNumber = o.OrderNumber ?? $"#GE-{o.Id}",
                    customerName = o.CustomerName,
                    productName = o.Product != null ? o.Product.Name : "Bag",
                    quantity = o.Quantity,
                    totalAmount = o.TotalAmount,
                    amountFormatted = "₹" + o.TotalAmount.ToString("N0"),
                    dateFormatted = o.OrderDate.ToString("dd-MMM-yyyy"),
                    status = o.Status
                })
            });
        }

        // ==========================================
        // API: WAREHOUSE STOCK INVENTORY (RED / GREEN HEALTH)
        // Down stock (< 12,000) = RED, High stock (> 25,000) = GREEN
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetStockInventory()
        {
            var products = await _context.Products
                .OrderBy(p => p.StockQuantity)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    description = p.Description,
                    bagSize = p.BagSize.ToString(),
                    stockQuantity = p.StockQuantity,
                    stockFormatted = p.StockQuantity.ToString("N0"),
                    customerPrice = p.CustomerPrice,
                    customerPriceFormatted = "₹" + p.CustomerPrice.ToString("F2"),
                    retailerPrice = p.RetailerPrice,
                    retailerPriceFormatted = "₹" + p.RetailerPrice.ToString("F2"),
                    isAvailable = p.IsAvailable,
                    stockLevel = p.StockQuantity < 12000 ? "Low" : p.StockQuantity > 25000 ? "High" : "Mid",
                    statusColor = p.StockQuantity < 12000 ? "danger" : p.StockQuantity > 25000 ? "success" : "warning",
                    statusLabel = p.StockQuantity < 12000 ? "Down Stock (Low)" : p.StockQuantity > 25000 ? "High Stock (Healthy)" : "Moderate Stock"
                })
                .ToListAsync();

            var totalStock = products.Sum(p => (long)p.stockQuantity);
            var lowStockCount = products.Count(p => p.stockQuantity < 12000);
            var midStockCount = products.Count(p => p.stockQuantity >= 12000 && p.stockQuantity <= 25000);
            var highStockCount = products.Count(p => p.stockQuantity > 25000);

            return Json(new
            {
                success = true,
                totalProducts = products.Count,
                totalStock = totalStock,
                totalStockFormatted = totalStock.ToString("N0"),
                lowStockCount = lowStockCount,
                midStockCount = midStockCount,
                highStockCount = highStockCount,
                products = products
            });
        }

        // ==========================================
        // API: QUICK RESTOCK / ADJUST STOCK (ADMIN ONLY)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> QuickUpdateStock(int productId, int quantity, string operation = "add")
        {
            var isAdmin = User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Admin");
            if (!isAdmin)
            {
                return Unauthorized(new { success = false, message = "Admin authorization required to adjust warehouse inventory." });
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound(new { success = false, message = "Bag product not found." });
            }

            if (operation == "add")
            {
                product.StockQuantity += quantity;
            }
            else if (operation == "subtract")
            {
                product.StockQuantity = Math.Max(0, product.StockQuantity - quantity);
            }
            else
            {
                product.StockQuantity = Math.Max(0, quantity);
            }

            await _context.SaveChangesAsync();

            var totalStock = await _context.Products.SumAsync(p => (long)p.StockQuantity);
            var lowStockCount = await _context.Products.CountAsync(p => p.StockQuantity < 12000);
            var highStockCount = await _context.Products.CountAsync(p => p.StockQuantity > 25000);

            return Json(new
            {
                success = true,
                productId = product.Id,
                name = product.Name,
                newStock = product.StockQuantity,
                newStockFormatted = product.StockQuantity.ToString("N0"),
                stockLevel = product.StockQuantity < 12000 ? "Low" : product.StockQuantity > 25000 ? "High" : "Mid",
                statusColor = product.StockQuantity < 12000 ? "danger" : product.StockQuantity > 25000 ? "success" : "warning",
                totalStock = totalStock,
                totalStockFormatted = totalStock.ToString("N0"),
                lowStockCount = lowStockCount,
                highStockCount = highStockCount,
                message = $"Successfully updated stock for '{product.Name}' to {product.StockQuantity:N0} pcs."
            });
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
