using System;
using System.Threading.Tasks;
using CardLister.Models;
using CardLister.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CardLister.ViewModels
{
    public partial class EditCardViewModel : ViewModelBase
    {
        private readonly ICardRepository _cardRepository;
        private readonly ILogger<EditCardViewModel> _logger;

        private Card? _originalCard;

        [ObservableProperty] private CardDetailViewModel? _cardDetail;
        [ObservableProperty] private string? _errorMessage;
        [ObservableProperty] private string? _successMessage;
        [ObservableProperty] private bool _isLoading;

        // Image previews from the original card
        [ObservableProperty] private string? _imagePathFront;
        [ObservableProperty] private string? _imagePathBack;

        public EditCardViewModel(ICardRepository cardRepository, ILogger<EditCardViewModel> logger)
        {
            _cardRepository = cardRepository;
            _logger = logger;
        }

        public async Task LoadCardAsync(int cardId)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                _originalCard = await _cardRepository.GetCardAsync(cardId);
                if (_originalCard == null)
                {
                    ErrorMessage = "Card not found.";
                    return;
                }

                CardDetail = CardDetailViewModel.FromCard(_originalCard);
                ImagePathFront = _originalCard.ImagePathFront;
                ImagePathBack = _originalCard.ImagePathBack;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load card {CardId} for editing", cardId);
                ErrorMessage = $"Failed to load card: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (CardDetail == null || _originalCard == null)
                return;

            try
            {
                ErrorMessage = null;
                SuccessMessage = null;

                var updated = CardDetail.ToCard();

                // Preserve fields that the form doesn't edit
                updated.Id = _originalCard.Id;
                updated.Status = _originalCard.Status;
                updated.CreatedAt = _originalCard.CreatedAt;
                updated.UpdatedAt = DateTime.UtcNow;
                updated.ImagePathFront = _originalCard.ImagePathFront;
                updated.ImagePathBack = _originalCard.ImagePathBack;
                updated.ImageUrl1 = _originalCard.ImageUrl1;
                updated.ImageUrl2 = _originalCard.ImageUrl2;
                updated.EstimatedValue = _originalCard.EstimatedValue;
                updated.PriceSource = _originalCard.PriceSource;
                updated.PriceDate = _originalCard.PriceDate;
                updated.ListingPrice = _originalCard.ListingPrice;
                updated.PriceCheckCount = _originalCard.PriceCheckCount;
                updated.SalePrice = _originalCard.SalePrice;
                updated.SaleDate = _originalCard.SaleDate;
                updated.SalePlatform = _originalCard.SalePlatform;
                updated.FeesPaid = _originalCard.FeesPaid;
                updated.ShippingCost = _originalCard.ShippingCost;
                updated.NetProfit = _originalCard.NetProfit;

                await _cardRepository.UpdateCardAsync(updated);

                _logger.LogInformation("Card {CardId} updated: {PlayerName}", updated.Id, updated.PlayerName);

                // Navigate back to Inventory
                if (App.Services.GetService(typeof(MainWindowViewModel)) is MainWindowViewModel mainVm)
                    mainVm.NavigateToCommand.Execute("Inventory");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save card {CardId}", _originalCard?.Id);
                ErrorMessage = $"Failed to save: {ex.Message}";
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            if (App.Services.GetService(typeof(MainWindowViewModel)) is MainWindowViewModel mainVm)
                mainVm.NavigateToCommand.Execute("Inventory");
        }
    }
}
