// ============================================================
// Program.cs — ENTERPRISE REWRITE
//
// FIXES:
//  1. Added role seeding (Admin, Teacher, Student) with correct names
//  2. Added admin bootstrap (first admin account creation)
//  3. Password policy improved for enterprise
//  4. Logging configured properly
//  5. Added FileUploadService to DI
//  6. External auth providers use null-safe configuration
//  7. Session config is explicit
//  8. Added global exception handler
// ============================================================

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using SmartClassAI.DataAccess.Data;
using SmartClassAI.DataAccess.Repository;
using SmartClassAI.DataAccess.Repository.IRepository;
using SmartClassAI.Models;
using SmartClassAI.Utility;
using SmartClassAI.WebApp.Filters;
using SmartClassAI.WebApp.Hubs;
using SmartClassAI.WebApp.Services;
using SmartClassAI.WebApp.Utility;

var builder = WebApplication.CreateBuilder(args);

// =============================================
// LOGGING
// =============================================
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// =============================================
// DATABASE
// =============================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)
    ));

// =============================================
// IDENTITY
// =============================================
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;

    // Enterprise password policy
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    // Lockout
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// =============================================
// COOKIE CONFIG
// =============================================
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.LogoutPath = "/Identity/Account/Logout";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

// =============================================
// EXTERNAL AUTH (null-safe)
// =============================================
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
        });
}

// =============================================
// MVC + RAZOR PAGES
// =============================================
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
    options.Filters.Add<SuspendedUserFilter>();
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", p => p.RequireRole(SD.Role_Admin));
    options.AddPolicy("RequireTeacher", p => p.RequireRole(SD.Role_Teacher, SD.Role_Admin));
});
builder.Services.AddRazorPages();

// =============================================
// SIGNALR
// =============================================
builder.Services.AddSignalR();

// =============================================
// SESSION
// =============================================
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// =============================================
// DEPENDENCY INJECTION
// =============================================
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<FileUploadService>();
builder.Services.AddScoped<ClassroomAccessService>();
builder.Services.AddScoped<SuspendedUserFilter>();
builder.Services.AddScoped<EmailTemplateService>();
// =============================================
// BUILD
// =============================================
var app = builder.Build();

// =============================================
// SEED ROLES + ADMIN (runs once at startup)
// =============================================
await SeedRolesAndAdminAsync(app);

// =============================================
// MIDDLEWARE PIPELINE
// =============================================
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// =============================================
// ROUTES
// =============================================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// =============================================
// SIGNALR HUBS
// =============================================
app.MapHub<ClassroomHub>("/classroomHub");
app.MapHub<AdminHub>("/adminHub");

app.Run();

// =============================================
// ROLE SEEDING
// FIX: Roles were never seeded — SD.Role_Admin etc. would
//      never match because the Identity roles table was empty
// =============================================
static async Task SeedRolesAndAdminAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    // Seed roles
    string[] roles = { SD.Role_Admin, SD.Role_Teacher, SD.Role_Student };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // Seed default admin account (from config)
    var adminEmail = config["AdminSeed:Email"] ?? "admin@smartclassai.com";
    var adminPassword = config["AdminSeed:Password"] ?? "Admin@123";

    var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
    if (existingAdmin == null)
    {
        var adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "System Administrator",
            IsActive = true,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, SD.Role_Admin);
        }
    }
}