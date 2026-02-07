using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CardLister.Helpers;
using CardLister.Models;
using CardLister.Models.Enums;
using CardLister.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CardLister.ViewModels
{
    public partial class PricingViewModel : ViewModelBase
    {
        private readonly ICardRepository _cardRepository;
        private readonly IPricerService _pricerService;
        private readonly IBrowserService _browserService;
        private readonly ISettingsService _settingsService;
        private readonly ISoldPriceService _soldPriceService;

        private List<Card> _unpricedCards = new();
        private int _currentIndex;

        [ObservableProperty] private Card? _currentCard;
        [ObservableProperty] private int _currentPosition;
        [ObservableProperty] private int _totalCount;
        [ObservableProperty] private decimal? _marketValue;
        [ObservableProperty] private decimal? _suggestedPrice;
        [ObservableProperty] private decimal? _listingPrice;
        [ObservableProperty] private decimal? _netAfterFees;
        [ObservableProperty] private decimal? _costBasis;
        [ObservableProperty] private CostSource? _costSource;
        [ObservableProperty] private string? _costNotes;
        [ObservableProperty] private string? _statusMessage;
        [ObservableProperty] private bool _hasCards;

        // Automated pricing properties
        [ObservableProperty] private bool _isLookingUpPrice;
        [ObservableProperty] private PriceLookupResult? _lookupResult;
        [ObservableProperty] private string _automatedStatusMessage = "";

        public PricingViewModel(
            ICardRepository cardRepository,
            IPricerService pricerService,
            IBrowserService browserService,
            ISettingsService settingsService,
            ISoldPriceService soldPriceService)
        {
            _cardRepository = cardRepository;
            _pricerService = pricerService;
            _browserService = browserService;
            _settingsService = settingsService;
            _soldPriceService = soldPriceService;

            LoadUnpricedAsync();
        }

        partial void OnMarketValueChanged(decimal? value)
        {
            if (value.HasValue && CurrentCard != null)
            {
                SuggestedPrice = _pricerService.SuggestPrice(value.Value, CurrentCard);
                ListingPrice = SuggestedPrice;
            }
            else
            {
                SuggestedPrice = null;
                ListingPrice = null;
            }
        }

        partial void OnListingPriceChanged(decimal? value)
        {
            if (value.HasValue)
            {
                var settings = _settingsService.Load();
                NetAfterFees = PriceCalculator.CalculateNet(value.Value, settings.WhatnotFeePercent);
            }
            else
            {
                NetAfterFees = null;
            }
        }

        private async void LoadUnpricedAsync()
        {
            try
            {
                _unpricedCards = await _cardRepository.GetAllCardsAsync(CardStatus.Draft);
                TotalCount = _unpricedCards.Count;
                _currentIndex = 0;
                HasCards = _unpricedCards.Count > 0;

                if (HasCards)
                    ShowCurrentCard();
                else
                    StatusMessage = "No cards need pricing. Scan some cards first!";
            }
            catch
            {
                StatusMessage = "Failed to load cards.";
            }
        }

        private void ShowCurrentCard()
        {
            if (_currentIndex >= 0 && _currentIndex < _unpricedCards.Count)
            {
                CurrentCard = _unpricedCards[_currentIndex];
                CurrentPosition = _currentIndex + 1;
                MarketValue = CurrentCard.EstimatedValue;
                ListingPrice = CurrentCard.ListingPrice;
                CostBasis = CurrentCard.CostBasis;
                CostSource = CurrentCard.CostSource;
                CostNotes = CurrentCard.CostNotes;
                StatusMessage = null;

                // Clear automated pricing state
                LookupResult = null;
                AutomatedStatusMessage = "";
            }
        }

        [RelayCommand]
        private void OpenTerapeak()
        {
            if (CurrentCard != null)
                _browserService.OpenUrl(_pricerService.BuildTerapeakUrl(CurrentCard));
        }

        [RelayCommand]
        private void OpenEbaySold()
        {
            if (CurrentCard != null)
                _browserService.OpenUrl(_pricerService.BuildEbaySoldUrl(CurrentCard));
        }

        [RelayCommand]
        private async Task GetMarketPriceAsync()
        {
            if (CurrentCard == null) return;

            IsLookingUpPrice = true;
            LookupResult = null;
            AutomatedStatusMessage = "Checking local database...";

            try
            {
                // First, check if we have recent local data (within 30 days)
                var hasRecentData = await _soldPriceService.HasRecentDataAsync(CurrentCard, daysOld: 30);

                if (!hasRecentData)
                {
                    AutomatedStatusMessage = "Scraping sold listings from 130point.com (this may take 15+ seconds)...";

                    var scrapeResult = await _soldPriceService.ScrapeSoldPricesAsync(CurrentCard, maxResults: 20);

                    if (!scrapeResult.Success)
                    {
                        AutomatedStatusMessage = $"Scraping failed: {scrapeResult.ErrorMessage}. Use manual Terapeak lookup instead.";
                        return;
                    }

                    AutomatedStatusMessage = $"Found {scrapeResult.RecordsFound} sold listing(s). Calculating market value...";
                }
                else
                {
                    AutomatedStatusMessage = "Found recent data in local database. Calculating market value...";
                }

                // Find matching records in local database
                var matches = await _soldPriceService.FindMatchingRecordsAsync(CurrentCard);

                // Calculate market value
                LookupResult = _soldPriceService.CalculateMarketValue(matches, CurrentCard);

                if (LookupResult.Success && LookupResult.MedianPrice.HasValue)
                {
                    // Update market value with the median price
                    MarketValue = LookupResult.MedianPrice;

                    var confidenceLabel = LookupResult.Confidence switch
                    {
                        PriceConfidence.High => "High",
                        PriceConfidence.Medium => "Medium",
                        PriceConfidence.Low => "Low",
                        _ => "Unknown"
                    };

                    AutomatedStatusMessage = $"Success! Found {LookupResult.SampleSize} comparable sale(s) - {confidenceLabel} confidence";
                }
                else
                {
                    AutomatedStatusMessage = "No comparable sales found. Use manual Terapeak lookup to research pricing.";
                }
            }
            catch (Exception ex)
            {
                AutomatedStatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLookingUpPrice = false;
            }
        }

        [RelayCommand]
        private async Task SaveAndNextAsync()
        {
            if (CurrentCard == null || !ListingPrice.HasValue) return;

            // Determine price source based on whether automated lookup was used
            var priceSource = LookupResult?.Success == true
                ? $"130point (auto, {LookupResult.SampleSize} sales, {LookupResult.Confidence})"
                : "Terapeak (manual)";

            CurrentCard.EstimatedValue = MarketValue;
            CurrentCard.ListingPrice = ListingPrice;
            CurrentCard.PriceSource = priceSource;
            CurrentCard.PriceDate = DateTime.UtcNow;
            CurrentCard.PriceCheckCount++;
            CurrentCard.Status = CardStatus.Priced;
            CurrentCard.CostBasis = CostBasis;
            CurrentCard.CostSource = CostSource;
            CurrentCard.CostNotes = CostNotes;

            await _cardRepository.UpdateCardAsync(CurrentCard);

            await _cardRepository.AddPriceHistoryAsync(new PriceHistory
            {
                CardId = CurrentCard.Id,
                EstimatedValue = MarketValue,
                ListingPrice = ListingPrice,
                PriceSource = priceSource
            });

            _unpricedCards.RemoveAt(_currentIndex);
            TotalCount = _unpricedCards.Count;

            if (_unpricedCards.Count == 0)
            {
                HasCards = false;
                CurrentCard = null;
                StatusMessage = "All cards priced!";
                return;
            }

            if (_currentIndex >= _unpricedCards.Count)
                _currentIndex = _unpricedCards.Count - 1;

            ShowCurrentCard();
        }

        [RelayCommand]
        private void Skip()
        {
            if (_unpricedCards.Count == 0) return;

            _currentIndex = (_currentIndex + 1) % _unpricedCards.Count;
            ShowCurrentCard();
        }

        [RelayCommand]
        private void Previous()
        {
            if (_unpricedCards.Count == 0) return;

            _currentIndex = (_currentIndex - 1 + _unpricedCards.Count) % _unpricedCards.Count;
            ShowCurrentCard();
        }
    }
}
