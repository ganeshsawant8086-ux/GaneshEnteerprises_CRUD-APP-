using GaneshEnterprises.Data;
using GaneshEnterprises.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GaneshEnterprises.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==========================================
        // 1. ADMIN DASHBOARD OVERVIEW
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var totalProducts = await _context.Products.CountAsync();
            var totalStock = await _context.Products.SumAsync(p => (long)p.StockQuantity);
            var totalRetailers = await _context.Retailers.CountAsync();
            var totalOrders = await _context.Orders.CountAsync();
            var pendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending");
            var totalRevenue = await _context.Orders.SumAsync(o => o.TotalAmount);

            ViewBag.TotalProducts = totalProducts;
            ViewBag.TotalStock = totalStock;
            ViewBag.TotalRetailers = totalRetailers;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.PendingOrders = pendingOrders;
            ViewBag.TotalRevenue = totalRevenue;

            var recentOrders = await _context.Orders
                .Include(o => o.Product)
                .Include(o => o.Retailer)
                .OrderByDescending(o => o.Id)
                .Take(7)
                .ToListAsync();

            var recentRetailers = await _context.Retailers
                .OrderByDescending(r => r.Id)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentRetailers = recentRetailers;
            return View(recentOrders);
        }

        // ==========================================
        // 2. PRODUCT & PRICING MANAGEMENT (ADMIN ONLY)
        // Admin manages Customer Price, Retailer Price & Margins
        // ==========================================
        public async Task<IActionResult> Products()
        {
            var products = await _context.Products
                .OrderBy(p => p.BagSize)
                .ThenBy(p => p.Name)
                .ToListAsync();
            return View(products);
        }

        [HttpGet]
        public IActionResult CreateProduct()
        {
            ViewBag.BagSizes = new SelectList(Enum.GetValues(typeof(BagSize)));
            return View(new Product { BagSize = BagSize.Medium, IsAvailable = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(Product product)
        {
            if (ModelState.IsValid)
            {
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Product '{product.Name}' added with both Customer (₹{product.CustomerPrice:F2}) and Retailer (₹{product.RetailerPrice:F2}) rates.";
                return RedirectToAction(nameof(Products));
            }
            ViewBag.BagSizes = new SelectList(Enum.GetValues(typeof(BagSize)), product.BagSize);
            return View(product);
        }

        [HttpGet]
        public async Task<IActionResult> EditProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            ViewBag.BagSizes = new SelectList(Enum.GetValues(typeof(BagSize)), product.BagSize);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(int id, Product product)
        {
            if (id != product.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Pricing for '{product.Name}' updated successfully (Customer: ₹{product.CustomerPrice:F2}, Retailer: ₹{product.RetailerPrice:F2}, Margin: ₹{product.Margin:F2}).";
                return RedirectToAction(nameof(Products));
            }

            ViewBag.BagSizes = new SelectList(Enum.GetValues(typeof(BagSize)), product.BagSize);
            return View(product);
        }

        // ==========================================
        // 3. RETAILER MANAGEMENT (10 RETAILERS)
        // ==========================================
        public async Task<IActionResult> Retailers()
        {
            var retailers = await _context.Retailers
                .Include(r => r.Orders)
                .OrderBy(r => r.BusinessName)
                .ToListAsync();
            return View(retailers);
        }

        [HttpGet]
        public IActionResult CreateRetailer()
        {
            return View(new Retailer { Password = "Password@123", IsActive = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRetailer(Retailer retailer)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Retailers.AnyAsync(r => r.Email.ToLower() == retailer.Email.ToLower()))
                {
                    ModelState.AddModelError("Email", "A retailer with this email already exists.");
                    return View(retailer);
                }

                _context.Retailers.Add(retailer);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Retailer '{retailer.BusinessName}' registered successfully. Default login password: {retailer.Password}";
                return RedirectToAction(nameof(Retailers));
            }
            return View(retailer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleRetailerStatus(int id)
        {
            var retailer = await _context.Retailers.FindAsync(id);
            if (retailer != null)
            {
                retailer.IsActive = !retailer.IsActive;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Retailer '{retailer.BusinessName}' is now {(retailer.IsActive ? "Active" : "Deactivated")}.";
            }
            return RedirectToAction(nameof(Retailers));
        }

        // ==========================================
        // 4. ORDER MANAGEMENT (20 ORDERS)
        // ==========================================
        public async Task<IActionResult> Orders(string? statusFilter)
        {
            ViewBag.CurrentStatus = statusFilter;
            var query = _context.Orders
                .Include(o => o.Product)
                .Include(o => o.Retailer)
                .Include(o => o.TrackingUpdates)
                .AsQueryable();

            if (!string.IsNullOrEmpty(statusFilter))
            {
                query = query.Where(o => o.Status == statusFilter);
            }

            var orders = await query.OrderByDescending(o => o.Id).ToListAsync();
            return View(orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                order.Status = status;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Order #{order.Id} status set to '{status}'.";
            }
            return RedirectToAction(nameof(Orders));
        }

        // ==========================================
        // 5. ORDER TRACKING MANAGEMENT (ONLY ADMIN)
        // ==========================================
        public async Task<IActionResult> Tracking()
        {
            var trackings = await _context.OrderTrackings
                .Include(t => t.Order)
                .ThenInclude(o => o!.Product)
                .OrderByDescending(t => t.UpdatedAt)
                .ToListAsync();
            return View(trackings);
        }

        [HttpGet]
        public async Task<IActionResult> ManageTracking(int? orderId)
        {
            var orders = await _context.Orders
                .Include(o => o.Product)
                .OrderByDescending(o => o.Id)
                .Select(o => new
                {
                    o.Id,
                    DisplayText = $"Order #{o.Id} - {o.CustomerName} ({o.Product!.Name})"
                })
                .ToListAsync();

            ViewBag.Orders = new SelectList(orders, "Id", "DisplayText", orderId);

            var tracking = new OrderTracking
            {
                OrderId = orderId ?? 1,
                TrackingNumber = $"TRK-GE-{new Random().Next(80000, 99999)}",
                CurrentStatus = "In Transit - Hub Dispatch",
                CurrentLocation = "Regional Logistics Facility",
                CarrierName = "Ganesh Express Logistics",
                EstimatedDelivery = DateTime.Today.AddDays(2),
                UpdatedAt = DateTime.Now
            };

            return View(tracking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageTracking(OrderTracking tracking)
        {
            if (ModelState.IsValid)
            {
                tracking.UpdatedAt = DateTime.Now;
                _context.OrderTrackings.Add(tracking);
                
                // Also update the order status
                var order = await _context.Orders.FindAsync(tracking.OrderId);
                if (order != null)
                {
                    if (tracking.CurrentStatus.Contains("Delivered"))
                    {
                        order.Status = "Delivered";
                    }
                    else if (tracking.CurrentStatus.Contains("Transit") || tracking.CurrentStatus.Contains("Dispatched"))
                    {
                        order.Status = "Shipped";
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Tracking update logged for Order #{tracking.OrderId} (Tracking #: {tracking.TrackingNumber}).";
                return RedirectToAction(nameof(Tracking));
            }

            var orders = await _context.Orders
                .Include(o => o.Product)
                .OrderByDescending(o => o.Id)
                .Select(o => new
                {
                    o.Id,
                    DisplayText = $"Order #{o.Id} - {o.CustomerName} ({o.Product!.Name})"
                })
                .ToListAsync();
            ViewBag.Orders = new SelectList(orders, "Id", "DisplayText", tracking.OrderId);
            return View(tracking);
        }
    }
}
