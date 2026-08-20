# AuthWeb — User Management & Authentication System (ITransition Task #4)

A complete, production-ready ASP.NET Core web application for user authentication, email verification, server-side request validation, and bulk user management built according to **ITransition Task #4** specifications.

[![GitHub Repository](https://img.shields.io/badge/GitHub-hydro1d%2Fauthweb__itransition-blue?logo=github)](https://github.com/hydro1d/authweb_itransition)
[![Framework](https://img.shields.io/badge/.NET-10.0-purple?logo=dotnet)](https://dotnet.microsoft.com/)
[![Database](https://img.shields.io/badge/EF%20Core-PostgreSQL%2FSQLite-blue?logo=postgresql)](https://ef.net)

---

## 🚀 Technology Stack

- **Backend**: C#, ASP.NET Core MVC 10.0
- **Data & ORM**: Entity Framework Core 10.0 with PostgreSQL (`Npgsql`) & SQLite fallback
- **Authentication**: ASP.NET Core Cookie Authentication with Secure Password Hashing (`IPasswordHasher<User>`)
- **Frontend**: Bootstrap 5, Bootstrap Icons, Clean HTML5/JavaScript
- **Testing**: xUnit, EF Core Sqlite In-Memory DB Tests

---

## 🏛️ Architecture & Key Features

1. **Explicit Database Unique Email Index (`IX_Users_Email`)**:
   - Configured in `ApplicationDbContext` via `HasIndex(u => u.Email).IsUnique()`.
   - Migration `InitialCreate` visibly creates `IX_Users_Email`.
   - Registration attempts direct `INSERT` without application-level pre-checks, catching `DbUpdateException` to display duplicate email messages.

2. **Server-Side Request Validation Middleware (`UserStatusValidationMiddleware`)**:
   - Runs on every HTTP request (except `/Account/Login` and `/Account/Register`).
   - Validates that the logged-in user still exists in the database and is NOT blocked.
   - If blocked or hard-deleted by another user/self-action, invalidates cookie authentication instantly and redirects to `/Account/Login?reason=blocked` or `/Account/Login?reason=deleted`.

3. **Email Confirmation System**:
   - Asynchronous confirmation token dispatch.
   - Confirmation link upgrades status from `Unverified` -> `Active`.
   - **Critical Rule**: If a user is `Blocked`, clicking the confirmation link retains `Blocked` status (blocked users cannot reactivate themselves via email links).

4. **Bulk User Management Table & Toolbar**:
   - Sortable responsive Bootstrap 5 table (sorted by `LastLoginAt` descending).
   - Checkbox column with header select-all/deselect-all support.
   - Standard JavaScript helper `getUniqIdValue(element)` to retrieve row identifiers.
   - Actions: **Block** (button with text), **Unblock** (icon), **Delete** (icon, hard delete), **Delete Unverified** (icon, hard delete).

5. **Hard Deletion**:
   - Deleted users are completely removed from the database (`RemoveRange`).
   - Deleted emails become immediately available for fresh registration.

---

## 📋 Git Branch Workflow

Work is organized across 9 clean, logical feature branches:

- `main` — Primary production branch
- `feature/project-setup` — Initial ASP.NET Core MVC structure
- `feature/database` — `User` model, `ApplicationDbContext`, EF Core migrations
- `feature/authentication` — Registration, Login, Logout, Cookie authentication
- `feature/email-confirmation` — Email service & async confirmation logic
- `feature/user-management` — User table UI, checkboxes, `getUniqIdValue` helper
- `feature/user-actions` — Bulk Block, Unblock, Delete, Delete Unverified logic
- `feature/request-validation` — Server-side `UserStatusValidationMiddleware`
- `feature/ui-polish` — Bootstrap styling, alerts, icons, tooltips
- `feature/testing` — Integration & unit test suite

---

## ⚙️ Environment Variables & Configuration

Set the following environment variables for production / SMTP deployment:

| Variable | Description | Example |
| :--- | :--- | :--- |
| `DATABASE_URL` | PostgreSQL connection URL | `postgres://user:pass@host:5432/dbname` |
| `ConnectionStrings:PostgreSQL` | Alternative PostgreSQL connection string | `Host=...;Database=...;Username=...;Password=...` |
| `SMTP_HOST` | SMTP server hostname | `smtp.gmail.com` |
| `SMTP_PORT` | SMTP port | `587` |
| `SMTP_USER` | SMTP username | `your-email@gmail.com` |
| `SMTP_PASS` | SMTP password / App Secret | `your-app-password` |
| `SMTP_FROM` | Sender address | `no-reply@authweb.com` |

*Note: If `DATABASE_URL` is omitted, the app automatically runs on local SQLite (`Data Source=authweb.db`).*

---

## 🛠️ How to Run Locally

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or .NET 8/9 SDK

### Steps
```bash
# 1. Clone repository
git clone https://github.com/hydro1d/authweb_itransition.git
cd authweb_itransition

# 2. Build application
dotnet build

# 3. Apply EF Core Migrations (automatic on application startup)
dotnet ef database update

# 4. Run application
dotnet run
```
Open your browser at `http://localhost:5000` or the port displayed in terminal.

---

## 🧪 How to Run Tests

```bash
dotnet test
```
Executes 4 automated unit & integration tests covering database unique constraints, hard delete re-registration, single-character password hashing, and status validation.

---

## 📽️ Video Recording Checklist Verification

When recording the final submission video, follow this sequence:
1. **Register User**: Register a new user (accepts 1-character password like `"a"`).
2. **Confirmation**: View the confirmation email dispatch message / link.
3. **Confirm Email**: Click confirmation link -> observe status change to `Active`.
4. **Login**: Sign in with new credentials -> land on User Management page.
5. **Block User**: Select a non-current user, click **Block** -> observe status changed to `Blocked`.
6. **Unblock User**: Select the blocked user, click **Unblock** -> observe status changed to `Active`.
7. **Select All & Self Block**: Select all users (including current user), click **Block** -> observe automatic redirection to Login page with blocked notice.
8. **Database Unique Index**: Open EF Migration `InitialCreate.cs` -> point out `migrationBuilder.CreateIndex(..., name: "IX_Users_Email", unique: true)`.
9. **Duplicate Exception Code**: Open `AccountController.cs` -> point out `IsUniqueConstraintViolation(ex)` and `_context.Users.Add(user); await _context.SaveChangesAsync();` direct INSERT without prior checking.
10. **Duplicate Registration Demo**: Register with an already existing email -> observe error message: `"An account with this email already exists."`.

---

## 🌐 Deployment Instructions

Deploy to **Render**, **Azure**, **AWS**, or **Railway**:
1. Connect GitHub repository `https://github.com/hydro1d/authweb_itransition.git`.
2. Build Command: `dotnet publish -c Release -o out`
3. Start Command: `dotnet out/AuthWeb.dll`
4. Set Environment Variables: `DATABASE_URL`, `SMTP_HOST`, `SMTP_PORT`, `SMTP_USER`, `SMTP_PASS`.

---

## 📄 License & Credits
Developed by **ITransition Intern** for Task #4 completion.
