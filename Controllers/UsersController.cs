using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AuthWeb.Data;
using AuthWeb.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthWeb.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // note: Display user management table sorted by LastLoginAt descending
            var users = await _context.Users
                .OrderByDescending(u => u.LastLoginAt.HasValue)
                .ThenByDescending(u => u.LastLoginAt)
                .ThenByDescending(u => u.RegisteredAt)
                .ToListAsync();

            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Block(List<Guid> userIds)
        {
            if (userIds == null || !userIds.Any())
            {
                TempData["ErrorMessage"] = "No users selected for blocking.";
                return RedirectToAction("Index");
            }

            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(currentUserIdStr, out Guid currentUserId);

            var usersToBlock = await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
            foreach (var user in usersToBlock)
            {
                user.Status = UserStatus.Blocked;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Successfully blocked {usersToBlock.Count} user(s).";

            // nota bene: If current logged-in user blocked themselves, sign out immediately
            if (userIds.Contains(currentUserId))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Account", new { reason = "blocked" });
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unblock(List<Guid> userIds)
        {
            if (userIds == null || !userIds.Any())
            {
                TempData["ErrorMessage"] = "No users selected for unblocking.";
                return RedirectToAction("Index");
            }

            var usersToUnblock = await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
            foreach (var user in usersToUnblock)
            {
                if (user.Status == UserStatus.Blocked)
                {
                    user.Status = UserStatus.Active;
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Successfully unblocked {usersToUnblock.Count} user(s).";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(List<Guid> userIds)
        {
            if (userIds == null || !userIds.Any())
            {
                TempData["ErrorMessage"] = "No users selected for deletion.";
                return RedirectToAction("Index");
            }

            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(currentUserIdStr, out Guid currentUserId);

            var usersToDelete = await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();

            // important: Hard delete users from database per specification
            _context.Users.RemoveRange(usersToDelete);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Successfully deleted {usersToDelete.Count} user(s).";

            // nota bene: If current logged-in user deleted themselves, sign out immediately
            if (userIds.Contains(currentUserId))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Account", new { reason = "deleted" });
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUnverified(List<Guid>? userIds)
        {
            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(currentUserIdStr, out Guid currentUserId);

            IQueryable<User> query = _context.Users.Where(u => u.Status == UserStatus.Unverified);

            // If specific user IDs passed, filter unverified within that selection
            if (userIds != null && userIds.Any())
            {
                query = query.Where(u => userIds.Contains(u.Id));
            }

            var unverifiedUsers = await query.ToListAsync();
            if (!unverifiedUsers.Any())
            {
                TempData["ErrorMessage"] = "No unverified users found to delete.";
                return RedirectToAction("Index");
            }

            var unverifiedIds = unverifiedUsers.Select(u => u.Id).ToList();

            // Hard delete unverified users from database
            _context.Users.RemoveRange(unverifiedUsers);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Successfully deleted {unverifiedUsers.Count} unverified user(s).";

            if (unverifiedIds.Contains(currentUserId))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Account", new { reason = "deleted" });
            }

            return RedirectToAction("Index");
        }
    }
}
