using System;
using System.Security.Claims;
using System.Threading.Tasks;
using AuthWeb.Data;
using AuthWeb.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AuthWeb.Middleware
{
    public class UserStatusValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public UserStatusValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
        {
            // note: Perform server-side validation on every request for authenticated users
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

                // Bypass registration and login actions
                if (!path.StartsWith("/account/login") && !path.StartsWith("/account/register"))
                {
                    var userIdStr = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (Guid.TryParse(userIdStr, out Guid userId))
                    {
                        // important: Check database to verify user still exists and is not blocked
                        var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);

                        if (user == null)
                        {
                            // nota bene: User was hard-deleted from database, invalidate cookie immediately
                            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                            context.Response.Redirect("/Account/Login?reason=deleted");
                            return;
                        }

                        if (user.Status == UserStatus.Blocked)
                        {
                            // nota bene: User is blocked, invalidate cookie immediately
                            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                            context.Response.Redirect("/Account/Login?reason=blocked");
                            return;
                        }
                    }
                }
            }

            await _next(context);
        }
    }
}
