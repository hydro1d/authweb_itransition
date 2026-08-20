using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AuthWeb.Data;
using AuthWeb.Models;
using AuthWeb.Services;
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
        private readonly IEmailSender _emailSender;

        public AccountController(
            ApplicationDbContext context,
            IPasswordHasher<User> passwordHasher,
            IEmailSender emailSender)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _emailSender = emailSender;
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

            var token = Guid.NewGuid().ToString("N");
            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = model.Name.Trim(),
                Email = model.Email.Trim().ToLowerInvariant(),
                Status = UserStatus.Unverified,
                RegisteredAt = DateTime.UtcNow,
                ConfirmationToken = token
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

            // important: We do NOT check if email exists prior to INSERT.
            // nota bene: Direct database INSERT attempt allows the database unique index constraint to enforce uniqueness.
            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Asynchronously send confirmation email
                var confirmationLink = Url.Action(
                    "ConfirmEmail",
                    "Account",
                    new { userId = user.Id, token = token },
                    Request.Scheme);

                _ = Task.Run(() => _emailSender.SendEmailConfirmationAsync(user.Email, user.Name, confirmationLink ?? string.Empty));

                TempData["SuccessMessage"] = "Registration successful! A confirmation email has been dispatched. (Check console logs if local SMTP is unconfigured)";
                TempData["DevConfirmationLink"] = confirmationLink;
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
        public async Task<IActionResult> ConfirmEmail(Guid userId, string token)
        {
            if (userId == Guid.Empty || string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Invalid confirmation link.";
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null || user.ConfirmationToken != token)
            {
                TempData["ErrorMessage"] = "Invalid or expired confirmation link.";
                return RedirectToAction("Login");
            }

            // important: If user is blocked, confirmation must NOT change blocked -> active. Blocked remains blocked!
            if (user.Status == UserStatus.Blocked)
            {
                user.ConfirmationToken = null; // consume token
                await _context.SaveChangesAsync();
                TempData["ErrorMessage"] = "Your email has been verified. However, your account is currently blocked by an administrator.";
                return RedirectToAction("Login");
            }

            // Change status from Unverified -> Active
            user.Status = UserStatus.Active;
            user.ConfirmationToken = null;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Email address confirmed successfully! Your status is now Active. You can log in.";
            return RedirectToAction("Login");
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

        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            return innerMessage.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) ||
                   innerMessage.Contains("23505", StringComparison.OrdinalIgnoreCase) ||
                   innerMessage.Contains("IX_Users_Email", StringComparison.OrdinalIgnoreCase) ||
                   innerMessage.Contains("duplicate key", StringComparison.OrdinalIgnoreCase);
        }
    }
}
