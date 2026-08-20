# AuthWeb — User Management & Authentication System (ITransition)

A complete, production-ready ASP.NET Core web application for user authentication, email verification, server-side request validation, and bulk user management built according to **ITransition** specifications.

[![GitHub Repository](https://img.shields.io/badge/GitHub-hydro1d%2Fauthweb__itransition-blue?logo=github)](https://github.com/hydro1d/authweb_itransition)
[![Framework](https://img.shields.io/badge/.NET-10.0-purple?logo=dotnet)](https://dotnet.microsoft.com/)
[![Database](https://img.shields.io/badge/EF%20Core-PostgreSQL%2FSQLite-blue?logo=postgresql)](https://ef.net)

---

##  Technology Stack

- **Backend**: C#, ASP.NET Core MVC 10.0
- **Data & ORM**: Entity Framework Core 10.0 with PostgreSQL (`Npgsql`) & SQLite fallback
- **Authentication**: ASP.NET Core Cookie Authentication with Secure Password Hashing (`IPasswordHasher<User>`)
- **Frontend**: Bootstrap 5, Bootstrap Icons, Clean HTML5/JavaScript
- **Testing**: xUnit, EF Core Sqlite In-Memory DB Tests

---

##  Architecture & Key Features

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


---

## 📄 License & Credits
Developed by **ITransition Intern** for Task #4 completion.
