using ArtemisBanking.Infrastructure;
using ArtemisBanking.Infrastructure.Seeds;
using ArtemisBanking.Infrastructure.Services;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHangfireServer(); // Debe ir aquí donde Hangfire.AspNetCore está disponible


builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.LoginPath        = "/Account/Login";
    opt.AccessDeniedPath = "/Account/AccessDenied";
    opt.ExpireTimeSpan   = TimeSpan.FromHours(8);
});

builder.Services.AddSession(opt =>
{
    opt.IdleTimeout        = TimeSpan.FromHours(8);
    opt.Cookie.HttpOnly    = true;
    opt.Cookie.IsEssential = true;
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
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

await DefaultUserSeeder.SeedAsync(app.Services);

// Hangfire Dashboard (solo en desarrollo) y job diario
if (app.Environment.IsDevelopment())
    app.UseHangfireDashboard("/hangfire");

RecurringJob.AddOrUpdate<LoanOverdueJob>(
    "mark-overdue-loan-entries",
    job => job.MarkOverdueEntriesAsync(),
    Cron.Daily); // Ejecuta cada día a medianoche UTC

app.MapAreaControllerRoute(
    name: "AdminArea",
    areaName: "Admin",
    pattern: "Admin/{controller=Home}/{action=Index}/{id?}");

app.MapAreaControllerRoute(
    name: "ClientArea",
    areaName: "Client",
    pattern: "Client/{controller=Home}/{action=Index}/{id?}");

app.MapAreaControllerRoute(
    name: "CashierArea",
    areaName: "Cashier",
    pattern: "Cashier/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();