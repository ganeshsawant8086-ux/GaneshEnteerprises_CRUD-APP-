using GaneshEnterprises.Data;
using GaneshEnterprises.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GaneshEnterprises.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(ApplicationDbContext context, ILogger<ProductsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Products (Customer Store Catalog)
        public async Task<IActionResult> Index(string? searchString, BagSize? bagSizeFilter, string? sortOrder)
        {
            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentSizeFilter"] = bagSizeFilter;
            ViewData["CurrentSort"] = sortOrder;

            ViewData["NameSortParam"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["PriceSortParam"] = sortOrder == "price_asc" ? "price_desc" : "price_asc";
            ViewData["StockSortParam"] = sortOrder == "stock_asc" ? "stock_desc" : "stock_asc";
            ViewData["SizeSortParam"] = sortOrder == "size_asc" ? "size_desc" : "size_asc";

            var query = _context.Products.Include(p => p.Orders).AsQueryable();

            // Filter by search text
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var term = searchString.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(term) || p.Description.ToLower().Contains(term));
            }

            // Filter by bag size (Small, Medium, Large)
            if (bagSizeFilter.HasValue)
            {
                query = query.Where(p => p.BagSize == bagSizeFilter.Value);
            }

            // Sorting logic
            query = sortOrder switch
            {
                "name_desc" => query.OrderByDescending(p => p.Name),
                "price_asc" => query.OrderBy(p => p.CustomerPrice),
                "price_desc" => query.OrderByDescending(p => p.CustomerPrice),
                "stock_asc" => query.OrderBy(p => p.StockQuantity),
                "stock_desc" => query.OrderByDescending(p => p.StockQuantity),
                "size_asc" => query.OrderBy(p => p.BagSize),
                "size_desc" => query.OrderByDescending(p => p.BagSize),
                _ => query.OrderBy(p => p.BagSize).ThenBy(p => p.Name)
            };

            var products = await query.ToListAsync();
            return View(products);
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Orders)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(product);
        }

        // GET: Products/Create (Admin only)
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewBag.BagSizes = new SelectList(Enum.GetValues(typeof(BagSize)));
            return View(new Product { BagSize = BagSize.Medium, IsAvailable = true });
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Product product)
        {
            if (product.CustomerPrice <= 0 && product.Price > 0)
            {
                product.CustomerPrice = product.Price;
            }
            if (product.RetailerPrice <= 0 && product.CustomerPrice > 0)
            {
                product.RetailerPrice = Math.Round(product.CustomerPrice * 0.75m, 2);
            }

            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Product '{product.Name}' was successfully created.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.BagSizes = new SelectList(Enum.GetValues(typeof(BagSize)), product.BagSize);
            return View(product);
        }

        // GET: Products/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.BagSizes = new SelectList(Enum.GetValues(typeof(BagSize)), product.BagSize);
            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.Id) return NotFound();

            if (product.CustomerPrice <= 0 && product.Price > 0)
            {
                product.CustomerPrice = product.Price;
            }
            if (product.RetailerPrice <= 0 && product.CustomerPrice > 0)
            {
                product.RetailerPrice = Math.Round(product.CustomerPrice * 0.75m, 2);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Product '{product.Name}' was successfully updated.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await ProductExists(product.Id)) return NotFound();
                    else throw;
                }
            }

            ViewBag.BagSizes = new SelectList(Enum.GetValues(typeof(BagSize)), product.BagSize);
            return View(product);
        }

        // GET: Products/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Orders)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products
                .Include(p => p.Orders)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction(nameof(Index));
            }

            if (product.Orders.Any())
            {
                TempData["ErrorMessage"] = $"Cannot delete '{product.Name}' because it has {product.Orders.Count} associated customer order(s).";
                return RedirectToAction(nameof(Details), new { id = product.Id });
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Product '{product.Name}' was successfully deleted.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> ProductExists(int id)
        {
            return await _context.Products.AnyAsync(e => e.Id == id);
        }
    }
}
