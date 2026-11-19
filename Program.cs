using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using G3AspNet.Authorization;
using G3AspNet.Data;
using G3AspNet.Models;
using G3AspNet.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("No se ha configurado la conexión DefaultConnection.");

// DbContext es scoped: una petición HTTP usa la misma unidad de trabajo de EF Core.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// Identity gestiona usuarios, cookies de sesión, contraseñas, 2FA y roles.
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/account/login";
    options.AccessDeniedPath = "/account/accessdenied";
    options.Cookie.Name = "G3AspNet.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Events.OnValidatePrincipal = async context =>
    {
        // Preserva la validación del SecurityStamp para que cambios de rol o baja invaliden cookies previas.
        await SecurityStampValidator.ValidatePrincipalAsync(context);
        if (context.Principal is null)
        {
            return;
        }

        var userManager = context.HttpContext.RequestServices
            .GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.GetUserAsync(context.Principal);
        if (user is null || !user.IsActive)
        {
            context.RejectPrincipal();
        }
    };
});

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromMinutes(2);
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy(PolicyNames.GestionTaller, policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole(RoleNames.Admin, RoleNames.Mechanic));

    options.AddPolicy(PolicyNames.SoloAdmin, policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole(RoleNames.Admin));
});

builder.Services.AddScoped<IAuthorizationHandler, VehicleAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, RepairAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, RepairPartAuthorizationHandler>();

builder.Services.AddControllersWithViews(options =>
{
    // Protege automáticamente los formularios POST contra peticiones falsificadas.
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.Configure<ImageStorageOptions>(
    builder.Configuration.GetSection("FileUploads:Images"));
var maxImageSize = builder.Configuration.GetValue(
    "FileUploads:Images:MaxFileSizeBytes",
    5 * 1024 * 1024);
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = maxImageSize + (512 * 1024);
});
builder.Services.AddSingleton<ImageStorage>();
builder.Services.AddScoped<UserService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

var appDataDirectory = Path.Combine(app.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(appDataDirectory);

using (var scope = app.Services.CreateScope())
{
    await DbInitializer.InitializeAsync(scope.ServiceProvider);
}

app.Run();

public partial class Program;
