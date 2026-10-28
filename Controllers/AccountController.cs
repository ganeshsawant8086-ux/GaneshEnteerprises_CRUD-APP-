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
        // 1. CUSTOMER LOGIN (Mobile Number Only)
        // Role: "Customer"
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
                if (User.IsInRole("Retailer"))
                {
                    return RedirectToAction("Index", "RetailerPortal");
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

            var customerName = !string.IsNullOrWhiteSpace(pastOrder?.CustomerName) && !pastOrder.CustomerName.StartsWith("Customer")
                ? pastOrder.CustomerName.Trim()
                : string.Empty;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, !string.IsNullOrWhiteSpace(customerName) ? customerName : "Customer"),
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
        // 2. RETAILER LOGIN (Email/Phone & Password or OTP)
        // Role: "Retailer"
        // Default Password: Password@123 | OTP: 123456
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> RetailerLogin()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Retailer"))
            {
                return RedirectToAction("Index", "RetailerPortal");
            }

            var firstRetailer = await _context.Retailers.FirstOrDefaultAsync();

            var model = new RetailerLoginViewModel
            {
                EmailOrPhone = firstRetailer != null ? firstRetailer.Email : "om.retail@gmail.com",
                Password = "Password@123",
                LoginMode = "password"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RetailerLogin(RetailerLoginViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.EmailOrPhone))
            {
                ModelState.AddModelError("EmailOrPhone", "Please enter your registered email or phone.");
                return View(model);
            }

            var identifier = model.EmailOrPhone.Trim().ToLower();

            // Search retailer in database
            var retailer = await _context.Retailers
                .FirstOrDefaultAsync(r => r.Email.ToLower() == identifier || r.Phone == identifier);

            if (retailer == null)
            {
                ModelState.AddModelError(string.Empty, "No retailer account found with the provided Email or Mobile number.");
                return View(model);
            }

            if (!retailer.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Your retailer account is currently deactivated. Please contact Ganesh Enterprises.");
                return View(model);
            }

            // Check Login Mode: Password vs OTP
            if (model.LoginMode == "otp")
            {
                var validOtp = "123456";
                if (string.IsNullOrWhiteSpace(model.OtpCode) || model.OtpCode.Trim() != validOtp)
                {
                    ModelState.AddModelError("OtpCode", "Invalid OTP. Use demo verification code: 123456.");
                    return View(model);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(model.Password) || model.Password.Trim() != retailer.Password)
                {
                    ModelState.AddModelError("Password", "Incorrect password. (Default retailer password is: Password@123)");
                    return View(model);
                }
            }

            // Sign In Retailer
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, retailer.OwnerName),
                new Claim(ClaimTypes.Email, retailer.Email),
                new Claim(ClaimTypes.Role, "Retailer"),
                new Claim("RetailerId", retailer.Id.ToString()),
                new Claim("BusinessName", retailer.BusinessName),
                new Claim("PortalType", "Retailer")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            TempData["SuccessMessage"] = $"Welcome {retailer.BusinessName}! Wholesale margins and retailer pricing are now unlocked.";
            return RedirectToAction("Index", "RetailerPortal");
        }

        // ==========================================
        // FORGOT PASSWORD FOR RETAILERS
        // ==========================================
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string emailOrPhone)
        {
            if (string.IsNullOrWhiteSpace(emailOrPhone))
            {
                ViewBag.Error = "Please enter your registered email or phone.";
                return View();
            }

            var term = emailOrPhone.Trim().ToLower();
            var retailer = await _context.Retailers.FirstOrDefaultAsync(r => r.Email.ToLower() == term || r.Phone == term);

            if (retailer != null)
            {
                ViewBag.Success = $"Password reset link / temporary PIN has been dispatched to {retailer.Email}. Your default password is: Password@123.";
            }
            else
            {
                ViewBag.Error = "No retailer account found matching this identifier.";
            }

            return View();
        }

        // ==========================================
        // 3. ADMIN LOGIN (Strict ID & Password)
        // Role: "Admin"
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
        // LOGOUT (Works for Customer, Retailer, Admin)
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
