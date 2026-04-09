using DairyProductApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<GoogleSheetsService>();

// Add session for admin login
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Initialize Google Sheets
using (var scope = app.Services.CreateScope())
{
    var sheetsService = scope.ServiceProvider.GetRequiredService<GoogleSheetsService>();
    await sheetsService.InitializeSheetsAsync();
    await sheetsService.SeedDefaultAdmin();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();

app.Run();
