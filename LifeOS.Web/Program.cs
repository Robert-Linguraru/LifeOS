using Hangfire;
using LifeOS.Core.Entities;
using LifeOS.Core.Interfaces;
using LifeOS.Infrastructure.Data;
using LifeOS.Infrastructure.Extensions;
using LifeOS.Infrastructure.Jobs;
using LifeOS.Infrastructure.Services;
using LifeOS.Web.Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();     

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IXPService, XPService>();
builder.Services.AddScoped<IDailyScoreJob, DailyScoreJob>();
builder.Services.AddScoped<IStreakBonusJob, StreakBonusJob>();
builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();
builder.Services.AddAntiforgery();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddHangfireWithPostgres(connectionString);

















var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.MapPost("/logout", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/login");
}).RequireAuthorization();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

RecurringJob.AddOrUpdate<IDailyScoreJob>(
    "midnight-daily-score",
    job => job.ExecuteAsync(CancellationToken.None),
    "0 0 * * *",
    new RecurringJobOptions
    {
        TimeZone = TimeZoneInfo.Local
    });
RecurringJob.AddOrUpdate<IStreakBonusJob>(
    "midnight-streak-bonus",
    job => job.ExecuteAsync(CancellationToken.None),
    "5 0 * * *", // runs at 00:05 daily — just after the daily score job
    new RecurringJobOptions
    {
        TimeZone = TimeZoneInfo.Local
    });
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<LifeOS.Web.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();