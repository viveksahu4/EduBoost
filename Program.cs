using EduBoost.Data;
using EduBoost.Middleware;
using EduBoost.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<IdentityUser>(o => o.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
        });
}

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Dependency Injection
builder.Services.AddScoped<ISlowLearnerService, SlowLearnerService>();
builder.Services.AddScoped<DbSeeder>();
builder.Services.AddHttpClient<IAiPlanService, AiPlanService>(c =>
{
    c.Timeout = TimeSpan.FromSeconds(25);
});
builder.Services.AddHttpClient<IGeminiChatService, GeminiChatService>(c =>
{
    c.Timeout = TimeSpan.FromSeconds(7);
});

// OpenAPI / Swagger Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "EduBoost Learning Analytics API",
        Version = "v1",
        Description = "REST API for slow-learner detection, assessment management, and remedial teaching."
    });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Enable Swagger and SwaggerUI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EduBoost API v1");
    c.RoutePrefix = "swagger";
});
app.UseSession();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");
app.MapRazorPages();

// Create/upgrade the database, seed demo data and create roles on startup.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var seeder = services.GetRequiredService<DbSeeder>();
        bool seeded = await seeder.SeedAsync();

        if (seeded)
        {
            var detector = services.GetRequiredService<ISlowLearnerService>();
            int flagged = await detector.RunDetectionAsync();
            logger.LogInformation("Demo data seeded. {Count} slow learners detected.", flagged);
        }

        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Admin", "Teacher" })
        {
            if (!await roles.RoleExistsAsync(role))
            {
                await roles.CreateAsync(new IdentityRole(role));
            }
        }

        // Development demo accounts: one Admin and one Teacher.
        if (app.Environment.IsDevelopment())
        {
            var users = services.GetRequiredService<UserManager<IdentityUser>>();
            await EnsureUserAsync(users, "admin@eduboost.local", "Admin@123", "Admin");
            await EnsureUserAsync(users, "teacher@eduboost.local", "Teacher@123", "Teacher");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database setup failed. Check the DefaultConnection string in appsettings.json.");
        throw;
    }
}

app.Run();

static async Task EnsureUserAsync(UserManager<IdentityUser> users, string email, string password, string role)
{
    var user = await users.FindByEmailAsync(email);
    if (user == null)
    {
        user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
        var created = await users.CreateAsync(user, password);
        if (!created.Succeeded) return;
    }

    if (!await users.IsInRoleAsync(user, role))
    {
        await users.AddToRoleAsync(user, role);
    }
}
