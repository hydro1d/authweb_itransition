using AuthWeb.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// note: Configure DbContext with PostgreSQL or SQLite fallback
var pgConnStr = builder.Configuration.GetConnectionString("PostgreSQL");
var dbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrEmpty(dbUrl))
{
    // Parse DATABASE_URL if in URI format (postgres://user:pass@host:port/db)
    pgConnStr = ParseDatabaseUrl(dbUrl);
}

if (!string.IsNullOrEmpty(pgConnStr) && !pgConnStr.Contains("localhost"))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(pgConnStr));
}
else
{
    var defaultConn = builder.Configuration.GetConnectionString("DefaultConnection") 
                      ?? "Data Source=authweb.db";
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(defaultConn));
}

var app = builder.Build();

// Auto-migrate database on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static string ParseDatabaseUrl(string url)
{
    if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
    {
        var userInfo = uri.UserInfo.Split(':');
        var user = userInfo.Length > 0 ? userInfo[0] : "";
        var pass = userInfo.Length > 1 ? userInfo[1] : "";
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var db = uri.AbsolutePath.TrimStart('/');
        return $"Host={host};Port={port};Database={db};Username={user};Password={pass};SSL Mode=Require;Trust Server Certificate=true;";
    }
    return url;
}
