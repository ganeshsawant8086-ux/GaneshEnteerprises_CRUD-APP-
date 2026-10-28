using System.Security.Claims;
using GaneshEnterprises.Data;
using GaneshEnterprises.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GaneshEnterprises.Controllers
{
    [Authorize(Roles = "Retailer")]
    public class RetailerPortalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RetailerPortalController> _logger;

        public RetailerPortalController(ApplicationDbContext context, ILogger<RetailerPortalController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==========================================
        // RETAILER DASHBOARD - SHOW MARGINS & PRODUCTS
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var retailerIdClaim = User.FindFirst("RetailerId")?.Value;
            int retailerId = int.TryParse(retailerIdClaim, out int rid) ? rid : 1;

            var retailer = await _context.Retailers.FindAsync(retailerId);
            if (retailer == null)
            {
                return RedirectToAction("RetailerLogin", "Account");
            }

            var products = await _context.Products
                .Where(p => p.IsAvailable)
                .OrderBy(p => p.BagSize)
                .ThenBy(p => p.Name)
                .ToListAsync();

            var orders = await _context.Orders
                .Include(o => o.Product)
                .Include(o => o.TrackingUpdates)
                .Where(o => o.RetailerId == retailerId || o.CustomerName == retailer.BusinessName)
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            var viewModel = new RetailerPortalViewModel
            {
                CurrentRetailer = retailer,
                Products = products,
                RetailerOrders = orders
            };

            return View(viewModel);
        }

        // ==========================================
        // PLACE BULK RETAILER ORDER (WHOLESALE RATE)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> PlaceOrder(int? productId)
        {
            var retailerIdClaim = User.FindFirst("RetailerId")?.Value;
            int retailerId = int.TryParse(retailerIdClaim, out int rid) ? rid : 1;
            var retailer = await _context.Retailers.FindAsync(retailerId);

            var products = await _context.Products
                .Where(p => p.IsAvailable)
                .OrderBy(p => p.BagSize)
                .Select(p => new
                {
                    p.Id,
                    DisplayText = $"{p.Name} [{p.BagSize}] - Wholesale: ₹{p.RetailerPrice:F2} (Customer MRP: ₹{p.CustomerPrice:F2} | Margin: ₹{p.Margin:F2}/pc)"
                })
                .ToListAsync();

            ViewBag.Products = new SelectList(products, "Id", "DisplayText", productId);

            var order = new Order
            {
                CustomerName = retailer?.BusinessName ?? "Retailer Order",
                ContactNumber = retailer?.Phone ?? "9876543210",
                OrderType = "Retailer",
                RetailerId = retailerId,
                Quantity = 2000,
                OrderDate = DateTime.Today,
                Status = "Pending"
            };

            if (productId.HasValue) order.ProductId = productId.Value;
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(Order order)
        {
            var retailerIdClaim = User.FindFirst("RetailerId")?.Value;
            int retailerId = int.TryParse(retailerIdClaim, out int rid) ? rid : 1;
            var retailer = await _context.Retailers.FindAsync(retailerId);

            var product = await _context.Products.FindAsync(order.ProductId);
            if (product == null)
            {
                ModelState.AddModelError("ProductId", "Selected product was not found.");
            }

            if (ModelState.IsValid && product != null)
            {
                order.OrderType = "Retailer";
                order.RetailerId = retailerId;
                order.CustomerName = retailer?.BusinessName ?? order.CustomerName;
                order.UnitPrice = product.RetailerPrice; // Wholesale discount for verified retailers
                order.TotalAmount = product.RetailerPrice * order.Quantity;
                order.OrderNumber = $"GE-RET-{new Random().Next(1000, 9999)}";

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Generate automatic tracking milestone
                var tracking = new OrderTracking
                {
                    OrderId = order.Id,
                    TrackingNumber = $"TRK-RET-{order.Id + 91000}",
                    CurrentStatus = "Wholesale Consignment Booked",
                    CurrentLocation = "Ganesh Enterprises Central Production Unit",
                    CarrierName = "Ganesh Express Wholesale Fleet",
                    EstimatedDelivery = DateTime.Today.AddDays(3),
                    UpdatedAt = DateTime.Now,
                    Notes = $"Retailer bulk shipment confirmed for {retailer?.BusinessName}."
                };
                _context.OrderTrackings.Add(tracking);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Wholesale Order #{order.Id} placed successfully at ₹{product.RetailerPrice:F2}/unit! Consignment Tracking Code: {tracking.TrackingNumber}";
                return RedirectToAction(nameof(Index));
            }

            var products = await _context.Products
                .Where(p => p.IsAvailable)
                .OrderBy(p => p.BagSize)
                .Select(p => new
                {
                    p.Id,
                    DisplayText = $"{p.Name} [{p.BagSize}] - Wholesale: ₹{p.RetailerPrice:F2} (Customer MRP: ₹{p.CustomerPrice:F2} | Margin: ₹{p.Margin:F2}/pc)"
                })
                .ToListAsync();

            ViewBag.Products = new SelectList(products, "Id", "DisplayText", order.ProductId);
            return View(order);
        }

        // ==========================================
        // RETAILER ORDER TRACKING
        // ==========================================
        public async Task<IActionResult> Tracking(string? code)
        {
            var retailerIdClaim = User.FindFirst("RetailerId")?.Value;
            int retailerId = int.TryParse(retailerIdClaim, out int rid) ? rid : 1;

            var query = _context.OrderTrackings
                .Include(t => t.Order)
                .ThenInclude(o => o!.Product)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(code))
            {
                var term = code.Trim().ToLower();
                query = query.Where(t => t.TrackingNumber.ToLower().Contains(term) ||
                                         t.OrderId.ToString() == term ||
                                         (t.Order != null && t.Order.CustomerName.ToLower().Contains(term)));
            }
            else
            {
                // Show tracking for retailer's own orders
                query = query.Where(t => t.Order != null && t.Order.RetailerId == retailerId);
            }

            var trackings = await query.OrderByDescending(t => t.UpdatedAt).ToListAsync();
            ViewBag.SearchCode = code;
            return View(trackings);
        }
    }
}
