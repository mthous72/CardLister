using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CardLister.Models;
using CardLister.Models.Enums;
using CardLister.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CardLister.ViewModels
{
    public partial class BulkScanViewModel : ViewModelBase
    {
        private readonly IScannerService _scannerService;
        private readonly ICardRepository _cardRepository;
        private readonly IFileDialogService _fileDialogService;
        private readonly ISettingsService _settingsService;
        private readonly IVariationVerifier _variationVerifier;
        private readonly ILogger<BulkScanViewModel> _logger;

        private CancellationTokenSource? _scanCts;

        [ObservableProperty] private bool _imagesArePairs = true;
        [ObservableProperty] private bool _isScanning;
        [ObservableProperty] private bool _isSaving;
        [ObservableProperty] private int _scanProgress;
        [ObservableProperty] private int _scanTotal;
        [ObservableProperty] private string? _errorMessage;
        [ObservableProperty] private string? _successMessage;
        [ObservableProperty] private BulkScanItem? _selectedItem;
        [ObservableProperty] private string? _statusMessage;

        public ObservableCollection<BulkScanItem> Items { get; } = new();

        public CardDetailViewModel? SelectedCard => SelectedItem?.CardDetail;

        partial void OnSelectedItemChanged(BulkScanItem? value)
        {
            OnPropertyChanged(nameof(SelectedCard));
        }

        public BulkScanViewModel(
            IScannerService scannerService,
            ICardRepository cardRepository,
            IFileDialogService fileDialogService,
            ISettingsService settingsService,
            IVariationVerifier variationVerifier,
            ILogger<BulkScanViewModel> logger)
        {
            _scannerService = scannerService;
            _cardRepository = cardRepository;
            _fileDialogService = fileDialogService;
            _settingsService = settingsService;
            _variationVerifier = variationVerifier;
            _logger = logger;
        }

        [RelayCommand]
        private async Task SelectImagesAsync()
        {
            var paths = await _fileDialogService.OpenImageFilesAsync();
            if (paths.Count == 0)
                return;

            ErrorMessage = null;
            SuccessMessage = null;

            paths.Sort(StringComparer.OrdinalIgnoreCase);

            if (ImagesArePairs)
            {
                // Pair consecutive images as front/back
                for (int i = 0; i < paths.Count; i += 2)
                {
                    var item = new BulkScanItem
                    {
                        Index = Items.Count + 1,
                        FrontImagePath = paths[i],
                        BackImagePath = i + 1 < paths.Count ? paths[i + 1] : null,
                        DisplayName = $"Card {Items.Count + 1}"
                    };
                    Items.Add(item);
                }
            }
            else
            {
                // Each image is a separate card (front only)
                foreach (var path in paths)
                {
                    var item = new BulkScanItem
                    {
                        Index = Items.Count + 1,
                        FrontImagePath = path,
                        DisplayName = $"Card {Items.Count + 1}"
                    };
                    Items.Add(item);
                }
            }
        }

        [RelayCommand]
        private async Task ScanAllAsync()
        {
            if (Items.Count == 0)
                return;

            var pending = Items.Where(i => i.Status == BulkScanStatus.Pending).ToList();
            if (pending.Count == 0)
                return;

            IsScanning = true;
            ErrorMessage = null;
            SuccessMessage = null;
            ScanProgress = 0;
            ScanTotal = pending.Count;
            _scanCts = new CancellationTokenSource();

            var settings = _settingsService.Load();

            foreach (var item in pending)
            {
                if (_scanCts.Token.IsCancellationRequested)
                    break;

                item.Status = BulkScanStatus.Scanning;
                StatusMessage = $"Scanning card {item.Index} of {ScanTotal}...";
                _logger.LogInformation("Scanning card {Index}: {Path}", item.Index, item.FrontImagePath);

                try
                {
                    var scanResult = await _scannerService.ScanCardAsync(
                        item.FrontImagePath, item.BackImagePath, settings.DefaultModel);

                    scanResult.Card.ImagePathFront = item.FrontImagePath;
                    if (!string.IsNullOrEmpty(item.BackImagePath))
                        scanResult.Card.ImagePathBack = item.BackImagePath;

                    item.CardDetail = CardDetailViewModel.FromCard(scanResult.Card);

                    // Run verification pipeline if enabled (same as regular Scan view)
                    if (settings.EnableVariationVerification && item.CardDetail != null)
                    {
                        try
                        {
                            var verification = await _variationVerifier.VerifyCardAsync(scanResult, item.FrontImagePath);

                            // Run confirmation pass if needed and enabled
                            if (settings.RunConfirmationPass && _variationVerifier.NeedsConfirmationPass(verification))
                            {
                                verification = await _variationVerifier.RunConfirmationPassAsync(scanResult, verification, item.FrontImagePath);
                            }

                            // Auto-apply high-confidence suggestions if enabled
                            if (settings.AutoApplyHighConfidenceSuggestions)
                            {
                                if (verification.SuggestedPlayerName != null &&
                                    verification.PlayerVerified == false &&
                                    verification.FieldConfidences.Any(f =>
                                        f.FieldName == "player_name" &&
                                        f.Confidence == VerificationConfidence.Conflict))
                                {
                                    item.CardDetail.PlayerName = verification.SuggestedPlayerName;
                                }

                                if (verification.SuggestedVariation != null &&
                                    verification.OverallConfidence != VerificationConfidence.Conflict)
                                {
                                    item.CardDetail.ParallelName = verification.SuggestedVariation;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Verification failed for card {Index}, using unverified scan", item.Index);
                        }
                    }

                    item.DisplayName = !string.IsNullOrEmpty(item.CardDetail.PlayerName)
                        ? item.CardDetail.PlayerName
                        : $"Card {item.Index}";
                    item.Status = BulkScanStatus.Scanned;
                    _logger.LogInformation("Successfully scanned card {Index}: {PlayerName}", item.Index, item.DisplayName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to scan card {Index}: {Path}", item.Index, item.FrontImagePath);
                    item.Status = BulkScanStatus.Error;
                    item.ErrorMessage = ex.Message;
                }

                ScanProgress++;

                // Add delay between requests ONLY for free models to avoid rate limiting
                // Paid models (with credits) don't have these strict limits
                var isFreeModel = settings.DefaultModel.Contains(":free");
                if (isFreeModel && ScanProgress < ScanTotal && !_scanCts.Token.IsCancellationRequested)
                {
                    StatusMessage = "Waiting 4 seconds to avoid free tier rate limits...";
                    _logger.LogInformation("Waiting 4 seconds before next scan to avoid rate limits on free model...");
                    await Task.Delay(4000, _scanCts.Token);
                }
            }

            IsScanning = false;
            _scanCts = null;
            StatusMessage = null;

            var scanned = Items.Count(i => i.Status == BulkScanStatus.Scanned);
            var errors = Items.Count(i => i.Status == BulkScanStatus.Error);

            if (errors > 0)
                ErrorMessage = $"Scanned {scanned} cards, {errors} failed";
            else
                SuccessMessage = $"Scanned {scanned} cards successfully";
        }

        [RelayCommand]
        private void CancelScan()
        {
            _scanCts?.Cancel();
        }

        [RelayCommand]
        private async Task SaveAllAsync()
        {
            var ready = Items.Where(i => i.Status == BulkScanStatus.Scanned && i.CardDetail != null).ToList();
            if (ready.Count == 0)
                return;

            IsSaving = true;
            ErrorMessage = null;
            SuccessMessage = null;
            int saved = 0;

            foreach (var item in ready)
            {
                try
                {
                    var card = item.CardDetail!.ToCard();
                    card.ImagePathFront = item.FrontImagePath;
                    card.ImagePathBack = item.BackImagePath;
                    card.Status = Models.Enums.CardStatus.Draft;
                    await _cardRepository.InsertCardAsync(card);

                    item.Status = BulkScanStatus.Saved;
                    saved++;
                }
                catch (Exception ex)
                {
                    item.Status = BulkScanStatus.Error;
                    item.ErrorMessage = ex.Message;
                }
            }

            IsSaving = false;
            SuccessMessage = $"Saved {saved} cards to My Cards!";
        }

        [RelayCommand]
        private void RemoveSelected()
        {
            if (SelectedItem == null)
                return;

            Items.Remove(SelectedItem);
            SelectedItem = null;

            // Re-index
            for (int i = 0; i < Items.Count; i++)
                Items[i].Index = i + 1;
        }

        [RelayCommand]
        private void ClearAll()
        {
            Items.Clear();
            SelectedItem = null;
            ErrorMessage = null;
            SuccessMessage = null;
            ScanProgress = 0;
            ScanTotal = 0;
        }
    }

    public enum BulkScanStatus
    {
        Pending,
        Scanning,
        Scanned,
        Saved,
        Error
    }

    public partial class BulkScanItem : ObservableObject
    {
        [ObservableProperty] private int _index;
        [ObservableProperty] private string _displayName = "Pending";
        [ObservableProperty] private BulkScanStatus _status = BulkScanStatus.Pending;
        [ObservableProperty] private string? _errorMessage;
        [ObservableProperty] private CardDetailViewModel? _cardDetail;

        public string FrontImagePath { get; set; } = string.Empty;
        public string? BackImagePath { get; set; }
    }
}
