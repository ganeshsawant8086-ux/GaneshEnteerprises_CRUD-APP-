using GaneshEnterprises.Data;
using GaneshEnterprises.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GaneshEnterprises.Controllers
{
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrdersController> _logger;

        private static readonly string[] OrderStatuses = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };

        public OrdersController(ApplicationDbContext context, ILogger<OrdersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Orders
        public async Task<IActionResult> Index(string? searchString, string? statusFilter, string? sortOrder)
        {
            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentStatusFilter"] = statusFilter;
            ViewData["CurrentSort"] = sortOrder;

            ViewData["DateSortParam"] = string.IsNullOrEmpty(sortOrder) ? "date_asc" : "";
            ViewData["CustomerSortParam"] = sortOrder == "customer_asc" ? "customer_desc" : "customer_asc";
            ViewData["QtySortParam"] = sortOrder == "qty_asc" ? "qty_desc" : "qty_asc";
            ViewData["StatusSortParam"] = sortOrder == "status_asc" ? "status_desc" : "status_asc";

            ViewBag.StatusList = OrderStatuses;

            var query = _context.Orders
                .Include(o => o.Product)
                .Include(o => o.TrackingUpdates)
                .Include(o => o.OrderItems)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var term = searchString.Trim().ToLower();
                query = query.Where(o => 
                    o.CustomerName.ToLower().Contains(term) ||
                    o.ContactNumber.Contains(term) ||
                    (o.OrderNumber != null && o.OrderNumber.ToLower().Contains(term)) ||
                    (o.Product != null && o.Product.Name.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(o => o.Status == statusFilter);
            }

            query = sortOrder switch
            {
                "date_asc" => query.OrderBy(o => o.OrderDate),
                "customer_asc" => query.OrderBy(o => o.CustomerName),
                "customer_desc" => query.OrderByDescending(o => o.CustomerName),
                "qty_asc" => query.OrderBy(o => o.Quantity),
                "qty_desc" => query.OrderByDescending(o => o.Quantity),
                "status_asc" => query.OrderBy(o => o.Status),
                "status_desc" => query.OrderByDescending(o => o.Status),
                _ => query.OrderByDescending(o => o.Id)
            };

            var orders = await query.ToListAsync();
            return View(orders);
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.Product)
                .Include(o => o.TrackingUpdates)
                .Include(o => o.Retailer)
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Order not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(order);
        }

        // GET: Orders/Create (No Login Required for Customers)
        public async Task<IActionResult> Create(int? productId)
        {
            await PopulateProductDropDownList(productId);
            ViewBag.StatusList = new SelectList(OrderStatuses, "Pending");

            var order = new Order
            {
                OrderDate = DateTime.Today,
                Status = "Pending",
                Quantity = 500,
                OrderType = "Customer"
            };

            if (productId.HasValue)
            {
                order.ProductId = productId.Value;
            }

            return View(order);
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Order order, string? cartItemsJson)
        {
            List<OrderItemDto>? cartItems = null;
            if (!string.IsNullOrWhiteSpace(cartItemsJson))
            {
                try
                {
                    cartItems = System.Text.Json.JsonSerializer.Deserialize<List<OrderItemDto>>(
                        cartItemsJson,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse cartItemsJson: {Json}", cartItemsJson);
                }
            }

            if (cartItems != null && cartItems.Count > 0)
            {
                ModelState.Remove("ProductId");
                ModelState.Remove("Quantity");

                if (string.IsNullOrWhiteSpace(order.CustomerName))
                {
                    ModelState.AddModelError("CustomerName", "Customer or Business Name is required.");
                }
                if (string.IsNullOrWhiteSpace(order.ContactNumber))
                {
                    ModelState.AddModelError("ContactNumber", "Contact number is required.");
                }

                if (ModelState.IsValid)
                {
                    var firstItem = cartItems[0];
                    order.ProductId = firstItem.ProductId;
                    order.Quantity = cartItems.Sum(c => c.Quantity);
                    order.UnitPrice = firstItem.UnitPrice;
                    order.TotalAmount = cartItems.Sum(c => c.TotalPrice);
                    order.OrderType = "Customer";
                    order.OrderNumber = $"GE-ORD-{new Random().Next(10000, 99999)}";

                    _context.Add(order);
                    await _context.SaveChangesAsync();

                    foreach (var item in cartItems)
                    {
                        var orderItem = new OrderItem
                        {
                            OrderId = order.Id,
                            ProductId = item.ProductId,
                            ProductName = item.ProductName,
                            BagSize = item.BagSize,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            TotalPrice = item.TotalPrice
                        };
                        _context.OrderItems.Add(orderItem);
                    }
                    await _context.SaveChangesAsync();

                    var tracking = new OrderTracking
                    {
                        OrderId = order.Id,
                        TrackingNumber = $"TRK-GE-{order.Id + 84000}",
                        CurrentStatus = "Order Placed & Confirmed",
                        CurrentLocation = "Ganesh Enterprises Central Hub",
                        CarrierName = "Ganesh Express Logistics",
                        EstimatedDelivery = DateTime.Today.AddDays(3),
                        UpdatedAt = DateTime.Now,
                        Notes = $"Customer order confirmed with {cartItems.Count} bag item(s) and scheduled for packaging."
                    };
                    _context.OrderTrackings.Add(tracking);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Order #{order.Id} placed successfully with {cartItems.Count} product(s)! Tracking Reference: {tracking.TrackingNumber}";
                    return RedirectToAction(nameof(Details), new { id = order.Id });
                }
            }
            else
            {
                var product = await _context.Products.FindAsync(order.ProductId);
                if (product == null)
                {
                    ModelState.AddModelError("ProductId", "Please select a plastic bag product or add items to cart.");
                }
                else if (!product.IsAvailable)
                {
                    ModelState.AddModelError("ProductId", $"'{product.Name}' is currently marked unavailable for orders.");
                }

                if (ModelState.IsValid && product != null)
                {
                    order.OrderType = "Customer";
                    order.UnitPrice = product.CustomerPrice > 0 ? product.CustomerPrice : product.Price;
                    order.TotalAmount = order.UnitPrice * order.Quantity;
                    order.OrderNumber = $"GE-ORD-{new Random().Next(10000, 99999)}";

                    _context.Add(order);
                    await _context.SaveChangesAsync();

                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = product.Id,
                        ProductName = product.Name,
                        BagSize = product.BagSize.ToString(),
                        Quantity = order.Quantity,
                        UnitPrice = order.UnitPrice,
                        TotalPrice = order.TotalAmount
                    };
                    _context.OrderItems.Add(orderItem);

                    var tracking = new OrderTracking
                    {
                        OrderId = order.Id,
                        TrackingNumber = $"TRK-GE-{order.Id + 84000}",
                        CurrentStatus = "Order Placed & Confirmed",
                        CurrentLocation = "Ganesh Enterprises Central Hub",
                        CarrierName = "Ganesh Express Logistics",
                        EstimatedDelivery = DateTime.Today.AddDays(3),
                        UpdatedAt = DateTime.Now,
                        Notes = "Customer order confirmed and scheduled for packaging."
                    };
                    _context.OrderTrackings.Add(tracking);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Order #{order.Id} placed successfully! Tracking Reference: {tracking.TrackingNumber}";
                    return RedirectToAction(nameof(Details), new { id = order.Id });
                }
            }

            await PopulateProductDropDownList(order.ProductId);
            ViewBag.StatusList = new SelectList(OrderStatuses, order.Status);
            return View(order);
        }

        // GET: Orders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Order not found.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateProductDropDownList(order.ProductId);
            ViewBag.StatusList = new SelectList(OrderStatuses, order.Status);
            return View(order);
        }

        // POST: Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Order order)
        {
            if (id != order.Id) return NotFound();

            var product = await _context.Products.FindAsync(order.ProductId);
            if (product == null)
            {
                ModelState.AddModelError("ProductId", "The selected product does not exist.");
            }

            if (ModelState.IsValid && product != null)
            {
                try
                {
                    if (order.UnitPrice <= 0)
                    {
                        order.UnitPrice = product.CustomerPrice;
                    }
                    order.TotalAmount = order.UnitPrice * order.Quantity;

                    _context.Update(order);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Order #{order.Id} was successfully updated.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await OrderExists(order.Id)) return NotFound();
                    else throw;
                }
            }

            await PopulateProductDropDownList(order.ProductId);
            ViewBag.StatusList = new SelectList(OrderStatuses, order.Status);
            return View(order);
        }

        // GET: Orders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Order not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Order #{id} was successfully deleted.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateProductDropDownList(object? selectedProduct = null)
        {
            var products = await _context.Products
                .OrderBy(p => p.BagSize)
                .ThenBy(p => p.Name)
                .ToListAsync();

            ViewBag.ProductsList = products;

            var productsQuery = products.Select(p => new
            {
                p.Id,
                DisplayText = $"{p.Name} [{p.BagSize} Bag] - ₹{(p.CustomerPrice > 0 ? p.CustomerPrice : p.Price):F2} (Stock: {p.StockQuantity})"
            }).ToList();

            ViewBag.ProductId = new SelectList(productsQuery, "Id", "DisplayText", selectedProduct);
        }

        private async Task<bool> OrderExists(int id)
        {
            return await _context.Orders.AnyAsync(e => e.Id == id);
        }
    }
}
