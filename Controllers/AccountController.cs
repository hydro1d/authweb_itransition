using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AuthWeb.Data;
using AuthWeb.Models;
using AuthWeb.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AccountController(ApplicationDbContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Users");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = model.Name.Trim(),
                Email = model.Email.Trim().ToLowerInvariant(),
                Status = UserStatus.Unverified,
                RegisteredAt = DateTime.UtcNow,
                ConfirmationToken = Guid.NewGuid().ToString("N")
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

            // important: We do NOT check if email exists prior to INSERT.
            // nota bene: Direct database INSERT attempt allows the database unique index constraint to enforce uniqueness.
            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // note: Confirmation email dispatch will be handled asynchronously.
                TempData["SuccessMessage"] = "Registration successful! You may log in now. A confirmation email has been dispatched.";
                return RedirectToAction("Login");
            }
            catch (DbUpdateException ex)
            {
                // Catch unique email constraint violation from database
                if (IsUniqueConstraintViolation(ex))
                {
                    ModelState.AddModelError("Email", "An account with this email already exists.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "An unexpected database error occurred during registration.");
                }

                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Login(string? reason)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Users");
            }

            if (reason == "blocked")
            {
                ViewBag.ErrorMessage = "Your account has been blocked or access has been revoked.";
            }
            else if (reason == "deleted")
            {
                ViewBag.ErrorMessage = "Your account no longer exists in the system.";
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var normalizedEmail = model.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            // Verify password
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            // check if user is blocked
            if (user.Status == UserStatus.Blocked)
            {
                ModelState.AddModelError(string.Empty, "Your account is currently blocked. Access denied.");
                return View(model);
            }

            // Update last login timestamp
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Create claims and sign in user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("Index", "Users");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Login");
        }

        // Helper function to identify unique index constraint violation
        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            return innerMessage.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) ||
                   innerMessage.Contains("23505", StringComparison.OrdinalIgnoreCase) || // Postgres SQLState unique_violation
                   innerMessage.Contains("IX_Users_Email", StringComparison.OrdinalIgnoreCase) ||
                   innerMessage.Contains("duplicate key", StringComparison.OrdinalIgnoreCase);
        }
    }
}
