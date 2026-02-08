using CardLister.Core.Data;
using CardLister.Core.Services;
using CardLister.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add DbContext with SQLite
var dbPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "CardLister", "cards.db");

// Ensure the directory exists
var dbDirectory = Path.GetDirectoryName(dbPath);
if (!Directory.Exists(dbDirectory))
{
    Directory.CreateDirectory(dbDirectory!);
}

builder.Services.AddDbContext<CardListerDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Add HttpClient and HttpContextAccessor
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

// Register Core services (shared with Desktop)
builder.Services.AddSingleton<ISettingsService, JsonSettingsService>();
builder.Services.AddScoped<ICardRepository, CardRepository>(); // Depends on DbContext
builder.Services.AddSingleton<IScannerService, OpenRouterScannerService>();
builder.Services.AddScoped<IPricerService, PricerService>(); // Depends on DbContext via repositories
builder.Services.AddScoped<IExportService, CsvExportService>(); // Depends on DbContext
builder.Services.AddSingleton<IImageUploadService, ImgBBUploadService>();
builder.Services.AddScoped<IVariationVerifier, VariationVerifierService>(); // Depends on DbContext
builder.Services.AddSingleton<IChecklistLearningService, ChecklistLearningService>(); // Uses IServiceProvider to create scopes
builder.Services.AddScoped<ISoldPriceService, Point130SoldPriceService>(); // Depends on DbContext
// Note: IEbayBrowseService not yet implemented, will add when ebay-browse-api feature merges

// Register web-specific services
builder.Services.AddSingleton<IFileDialogService, WebFileUploadService>();
builder.Services.AddSingleton<IBrowserService, JavaScriptBrowserService>();
builder.Services.AddSingleton<INavigationService, MvcNavigationService>();

var app = builder.Build();

// Enable WAL mode for shared database
using (var connection = new SqliteConnection($"Data Source={dbPath}"))
{
    connection.Open();
    var command = connection.CreateCommand();
    command.CommandText = "PRAGMA journal_mode = WAL;";
    command.ExecuteNonQuery();
}

// Initialize database (create tables, seed data)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CardListerDbContext>();
    db.Database.EnsureCreated();
    await SchemaUpdater.EnsureVerificationTablesAsync(db);
    await ChecklistSeeder.SeedIfEmptyAsync(db);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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
