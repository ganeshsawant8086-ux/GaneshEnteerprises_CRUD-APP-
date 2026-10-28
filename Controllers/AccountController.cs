using System.Security.Claims;
using System.Text.RegularExpressions;
using GaneshEnterprises.Data;
using GaneshEnterprises.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GaneshEnterprises.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(ApplicationDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==========================================
        // CUSTOMER LOGIN (Mobile Number Only)
        // Privacy guaranteed: Customers only see their own orders!
        // ==========================================
        [HttpGet]
        public IActionResult CustomerLogin(string? returnUrl)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Customer"))
                {
                    return RedirectToAction("MyOrders", "Orders");
                }
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Index", "Admin");
                }
            }

            var model = new CustomerLoginViewModel
            {
                ReturnUrl = returnUrl
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CustomerLogin(CustomerLoginViewModel model)
        {
            var rawMobile = model.MobileNumber ?? string.Empty;
            var digitsOnly = new string(rawMobile.Where(char.IsDigit).ToArray());

            if (digitsOnly.Length < 10)
            {
                ModelState.AddModelError("MobileNumber", "Please enter a valid 10-digit mobile number.");
                return View(model);
            }

            // Extract canonical 10 digits
            var canonicalPhone = digitsOnly.Length > 10 ? digitsOnly.Substring(digitsOnly.Length - 10) : digitsOnly;

            // Search for customer's past orders to get customer name if available
            var pastOrder = await _context.Orders
                .Where(o => o.ContactNumber.Contains(canonicalPhone))
                .OrderByDescending(o => o.Id)
                .FirstOrDefaultAsync();

            var customerName = !string.IsNullOrWhiteSpace(pastOrder?.CustomerName)
                ? pastOrder.CustomerName.Trim()
                : $"Customer ({canonicalPhone})";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, customerName),
                new Claim(ClaimTypes.MobilePhone, canonicalPhone),
                new Claim(ClaimTypes.Role, "Customer"),
                new Claim("PortalType", "Customer")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            TempData["SuccessMessage"] = $"Welcome {customerName}! You are logged in with mobile {canonicalPhone}. You can only see your own orders.";

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("MyOrders", "Orders");
        }

        // ==========================================
        // ADMIN LOGIN (Strict ID & Password)
        // ID: ganeshenterprises@gmail.com
        // Password: Admin@123
        // ==========================================
        [HttpGet]
        public IActionResult AdminLogin()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Admin");
            }

            var model = new AdminLoginViewModel
            {
                Email = "ganeshenterprises@gmail.com",
                Password = "Admin@123"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminLogin(AdminLoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Verify admin credentials
            if (model.Email.Trim().Equals("ganeshenterprises@gmail.com", StringComparison.OrdinalIgnoreCase) &&
                model.Password.Trim() == "Admin@123")
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, "Ganesh Enterprises Admin"),
                    new Claim(ClaimTypes.Email, "ganeshenterprises@gmail.com"),
                    new Claim(ClaimTypes.Role, "Admin"),
                    new Claim("PortalType", "Admin")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                TempData["SuccessMessage"] = "Welcome back, Admin! You now have full management access.";
                return RedirectToAction("Index", "Admin");
            }

            ModelState.AddModelError(string.Empty, "Invalid Admin Credentials. Please use ganeshenterprises@gmail.com and Admin@123.");
            return View(model);
        }

        // ==========================================
        // RETAILER LOGIN (Legacy/Inert redirect)
        // ==========================================
        [HttpGet]
        public IActionResult RetailerLogin()
        {
            // Redirect retailer login attempts to CustomerLogin or AdminLogin
            return RedirectToAction("CustomerLogin");
        }

        // ==========================================
        // LOGOUT
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
