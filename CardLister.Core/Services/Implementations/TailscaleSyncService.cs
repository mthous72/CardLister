using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CardLister.Core.Models;
using Microsoft.Extensions.Logging;

namespace CardLister.Core.Services
{
    public class TailscaleSyncService : ISyncService
    {
        private readonly ICardRepository _localRepo;
        private readonly ISettingsService _settings;
        private readonly HttpClient _httpClient;
        private readonly ILogger<TailscaleSyncService> _logger;

        public TailscaleSyncService(
            ICardRepository localRepo,
            ISettingsService settings,
            HttpClient httpClient,
            ILogger<TailscaleSyncService> logger)
        {
            _localRepo = localRepo;
            _settings = settings;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<SyncStatus> GetStatusAsync()
        {
            var settings = _settings.Load();
            if (!settings.EnableSync || string.IsNullOrEmpty(settings.SyncServerUrl))
            {
                return new SyncStatus
                {
                    IsConnected = false,
                    LastSync = settings.LastSyncTime
                };
            }

            try
            {
                var statusUrl = $"{settings.SyncServerUrl}/api/sync/status";
                var response = await _httpClient.GetFromJsonAsync<SyncStatusResponse>(statusUrl);

                return new SyncStatus
                {
                    IsConnected = true,
                    LastSync = settings.LastSyncTime,
                    ServerLastUpdated = response?.LastUpdated,
                    ServerCardCount = response?.CardCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get sync status from server");
                return new SyncStatus
                {
                    IsConnected = false,
                    LastSync = settings.LastSyncTime
                };
            }
        }

        public async Task<SyncResult> SyncAsync()
        {
            var settings = _settings.Load();
            if (!settings.EnableSync || string.IsNullOrEmpty(settings.SyncServerUrl))
            {
                return new SyncResult
                {
                    Success = false,
                    ErrorMessage = "Sync not configured. Enable sync in Settings."
                };
            }

            try
            {
                _logger.LogInformation("Starting sync with {ServerUrl}", settings.SyncServerUrl);

                // 1. Check server status
                var statusUrl = $"{settings.SyncServerUrl}/api/sync/status";
                var status = await _httpClient.GetFromJsonAsync<SyncStatusResponse>(statusUrl);
                if (status == null)
                {
                    return new SyncResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to connect to sync server"
                    };
                }

                _logger.LogInformation("Server status: {CardCount} cards, last updated {LastUpdated}",
                    status.CardCount, status.LastUpdated);

                // 2. Push local changes (cards updated since last sync)
                var lastSync = settings.LastSyncTime ?? DateTime.MinValue;
                var localChanges = (await _localRepo.GetAllCardsAsync())
                    .Where(c => c.UpdatedAt > lastSync)
                    .ToList();

                _logger.LogInformation("Found {Count} local changes to push", localChanges.Count);

                if (localChanges.Any())
                {
                    var pushUrl = $"{settings.SyncServerUrl}/api/sync/push";
                    var pushResponse = await _httpClient.PostAsJsonAsync(pushUrl, localChanges);
                    pushResponse.EnsureSuccessStatusCode();

                    var pushResult = await pushResponse.Content.ReadFromJsonAsync<PushResponse>();
                    _logger.LogInformation("Pushed {Synced} cards, {Failed} failed",
                        pushResult?.Synced ?? 0, pushResult?.Failed ?? 0);
                }

                // 3. Pull remote changes
                var pullUrl = $"{settings.SyncServerUrl}/api/sync/cards?since={lastSync:O}";
                var remoteCards = await _httpClient.GetFromJsonAsync<List<Card>>(pullUrl);

                _logger.LogInformation("Pulled {Count} remote changes", remoteCards?.Count ?? 0);

                // 4. Apply remote changes (merge by UpdatedAt timestamp)
                var pulledCount = 0;
                if (remoteCards != null)
                {
                    foreach (var remoteCard in remoteCards)
                    {
                        var localCard = await _localRepo.GetCardAsync(remoteCard.Id);

                        // Simple merge: newer UpdatedAt wins (user uses one computer at a time)
                        if (localCard == null || remoteCard.UpdatedAt > localCard.UpdatedAt)
                        {
                            await _localRepo.UpdateCardAsync(remoteCard);
                            pulledCount++;
                        }
                    }
                }

                // 5. Update last sync time
                settings.LastSyncTime = DateTime.UtcNow;
                _settings.Save(settings);

                _logger.LogInformation("Sync complete: pushed {Pushed}, pulled {Pulled}",
                    localChanges.Count, pulledCount);

                return new SyncResult
                {
                    Success = true,
                    CardsPushed = localChanges.Count,
                    CardsPulled = pulledCount,
                    SyncTime = DateTime.UtcNow
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to connect to sync server");
                return new SyncResult
                {
                    Success = false,
                    ErrorMessage = $"Cannot connect to server: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sync failed");
                return new SyncResult
                {
                    Success = false,
                    ErrorMessage = $"Sync failed: {ex.Message}"
                };
            }
        }

        private class SyncStatusResponse
        {
            public string? Status { get; set; }
            public DateTime LastUpdated { get; set; }
            public int CardCount { get; set; }
            public DateTime ServerTime { get; set; }
        }

        private class PushResponse
        {
            public int Synced { get; set; }
            public int Failed { get; set; }
            public List<string>? Errors { get; set; }
        }
    }
}
