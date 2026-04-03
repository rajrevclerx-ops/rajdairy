using DairyProductApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register Google Sheets Service
builder.Services.AddSingleton<GoogleSheetsService>();

var app = builder.Build();

// Initialize Google Sheets (create tabs & headers if missing)
using (var scope = app.Services.CreateScope())
{
    var sheetsService = scope.ServiceProvider.GetRequiredService<GoogleSheetsService>();
    await sheetsService.InitializeSheetsAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
