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

            var deliveredToday = deliveredOrders.Where(o => o.OrderDate >= today && o.OrderDate < tomorrow).Sum(o => o.TotalAmount);
            var countToday = deliveredOrders.Count(o => o.OrderDate >= today && o.OrderDate < tomorrow);

            var deliveredMonth = deliveredOrders.Where(o => o.OrderDate >= firstDayOfMonth && o.OrderDate < firstDayOfNextMonth).Sum(o => o.TotalAmount);
            var countMonth = deliveredOrders.Count(o => o.OrderDate >= firstDayOfMonth && o.OrderDate < firstDayOfNextMonth);

            var deliveredYear = deliveredOrders.Where(o => o.OrderDate >= firstDayOfYear && o.OrderDate < firstDayOfNextYear).Sum(o => o.TotalAmount);
            var countYear = deliveredOrders.Count(o => o.OrderDate >= firstDayOfYear && o.OrderDate < firstDayOfNextYear);

            var deliveredAllTime = deliveredOrders.Sum(o => o.TotalAmount);
            var countAllTime = deliveredOrders.Count;

            ViewBag.TotalProducts = totalProducts;
            ViewBag.TotalStock = totalStock;
            ViewBag.TotalRetailers = totalRetailers;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.PendingOrders = pendingOrders;
            ViewBag.TotalRevenue = totalRevenue;

            ViewBag.DeliveredBenefitToday = deliveredToday;
            ViewBag.DeliveredBenefitMonth = deliveredMonth;
            ViewBag.DeliveredBenefitYear = deliveredYear;
            ViewBag.DeliveredBenefitAllTime = deliveredAllTime;

            ViewBag.DeliveredCountToday = countToday;
            ViewBag.DeliveredCountMonth = countMonth;
            ViewBag.DeliveredCountYear = countYear;
            ViewBag.DeliveredCountAllTime = countAllTime;

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
        // API: REAL-TIME DELIVERED BENEFIT STATS (AUTO MODE)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetDeliveredBenefitStats()
        {
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
        // AJAX QUICK STATUS UPDATE (Delivered Update in Auto Mode)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> QuickUpdateStatusAjax(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                order.Status = status;
                await _context.SaveChangesAsync();
                return Json(new { success = true, orderId = id, status = status });
            }
            return Json(new { success = false, message = "Order not found." });
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
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Product '{product.Name}' updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Products.AnyAsync(e => e.Id == product.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Products));
            }
            ViewBag.BagSizes = new SelectList(Enum.GetValues(typeof(BagSize)), product.BagSize);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Product '{product.Name}' deleted.";
            }
            return RedirectToAction(nameof(Products));
        }

        // ==========================================
        // 3. RETAILER ACCOUNTS MANAGEMENT (ADMIN ONLY)
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
            return View(new Retailer { IsActive = true, State = "Maharashtra" });
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
                TempData["SuccessMessage"] = $"Retailer '{retailer.BusinessName}' registered successfully.";
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
                TempData["SuccessMessage"] = $"Retailer '{retailer.BusinessName}' status changed to {(retailer.IsActive ? "Active" : "Inactive")}.";
            }
            return RedirectToAction(nameof(Retailers));
        }

        // ==========================================
        // 4. ORDERS PIPELINE MANAGEMENT (ADMIN ONLY)
        // ==========================================
        public async Task<IActionResult> Orders(string? statusFilter, string? orderType)
        {
            var query = _context.Orders
                .Include(o => o.Product)
                .Include(o => o.Retailer)
                .AsQueryable();

            if (!string.IsNullOrEmpty(statusFilter))
            {
                query = query.Where(o => o.Status == statusFilter);
            }

            if (!string.IsNullOrEmpty(orderType))
            {
                if (orderType.Equals("Retailer", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(o => o.OrderType == "Retailer" || o.RetailerId != null);
                }
                else if (orderType.Equals("Customer", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(o => o.OrderType == "Customer" && o.RetailerId == null);
                }
            }

            var orders = await query
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            ViewBag.CurrentStatus = statusFilter;
            ViewBag.CurrentOrderType = orderType;
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

        // ==========================================
        // 6. WAREHOUSE STOCK INVENTORY API (RED/GREEN)
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

        [HttpPost]
        public async Task<IActionResult> QuickUpdateStock(int productId, int quantity, string operation = "add")
        {
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
    }
}
