using CardLister.Core.Data;
using CardLister.Core.Models;
using CardLister.Core.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Get database path from environment or use default
var dbPath = Environment.GetEnvironmentVariable("CARDLISTER_DB_PATH")
    ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CardLister", "cards.db");

// Add CardLister.Core services
builder.Services.AddDbContext<CardListerDbContext>(options =>
{
    options.UseSqlite($"Data Source={dbPath}");
});
builder.Services.AddScoped<ICardRepository, CardRepository>();

// Add CORS for local network access
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Enable CORS
app.UseCors();

// Sync endpoints
app.MapGet("/api/sync/status", async (ICardRepository repo) =>
{
    var lastUpdated = await repo.GetAllCardsAsync();
    var lastUpdatedTime = lastUpdated.Any()
        ? lastUpdated.Max(c => c.UpdatedAt)
        : DateTime.MinValue;

    return Results.Ok(new
    {
        Status = "Ready",
        LastUpdated = lastUpdatedTime,
        CardCount = lastUpdated.Count,
        ServerTime = DateTime.UtcNow
    });
})
.WithName("GetSyncStatus")
.WithOpenApi();

app.MapGet("/api/sync/cards", async (
    ICardRepository repo,
    DateTime? since) =>
{
    var cards = since.HasValue
        ? (await repo.GetAllCardsAsync())
            .Where(c => c.UpdatedAt > since.Value)
            .ToList()
        : await repo.GetAllCardsAsync();

    return Results.Ok(cards);
})
.WithName("GetCards")
.WithOpenApi();

app.MapPost("/api/sync/push", async (
    ICardRepository repo,
    List<Card> cards) =>
{
    var synced = 0;
    var errors = new List<string>();

    foreach (var card in cards)
    {
        try
        {
            // Check if card exists
            var existing = await repo.GetCardAsync(card.Id);
            if (existing != null)
            {
                // Update existing card
                await repo.UpdateCardAsync(card);
            }
            else
            {
                // This shouldn't happen in normal sync, but handle it
                // Can't insert with existing ID, so skip
                errors.Add($"Card {card.Id} not found on server - skipping");
                continue;
            }
            synced++;
        }
        catch (Exception ex)
        {
            errors.Add($"Card {card.Id}: {ex.Message}");
        }
    }

    return Results.Ok(new
    {
        Synced = synced,
        Failed = errors.Count,
        Errors = errors
    });
})
.WithName("PushCards")
.WithOpenApi();

Console.WriteLine($"CardLister Sync API");
Console.WriteLine($"Database: {dbPath}");
Console.WriteLine($"Listening on: http://0.0.0.0:5000");
Console.WriteLine($"Access via Tailscale IP on port 5000");

app.Run("http://0.0.0.0:5000");
