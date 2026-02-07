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

                // Update the tracked entity's properties instead of creating a new instance
                _originalCard.PlayerName = CardDetail.PlayerName ?? string.Empty;
                _originalCard.Sport = CardDetail.Sport;
                _originalCard.Brand = CardDetail.Brand;
                _originalCard.Manufacturer = CardDetail.Manufacturer;
                _originalCard.Year = CardDetail.Year;
                _originalCard.CardNumber = CardDetail.CardNumber;
                _originalCard.Team = CardDetail.Team;
                _originalCard.SetName = CardDetail.SetName;
                _originalCard.VariationType = CardDetail.VariationType;
                _originalCard.ParallelName = CardDetail.ParallelName;
                _originalCard.SerialNumbered = CardDetail.SerialNumbered;
                _originalCard.IsShortPrint = CardDetail.IsShortPrint;
                _originalCard.IsSSP = CardDetail.IsSSP;
                _originalCard.IsRookie = CardDetail.IsRookie;
                _originalCard.IsAuto = CardDetail.IsAuto;
                _originalCard.IsRelic = CardDetail.IsRelic;
                _originalCard.Condition = CardDetail.Condition;
                _originalCard.IsGraded = CardDetail.IsGraded;
                _originalCard.GradeCompany = CardDetail.GradeCompany;
                _originalCard.GradeValue = CardDetail.GradeValue;
                _originalCard.CertNumber = CardDetail.CertNumber;
                _originalCard.AutoGrade = CardDetail.AutoGrade;
                _originalCard.CostBasis = CardDetail.CostBasis;
                _originalCard.CostSource = CardDetail.CostSource;
                _originalCard.CostDate = CardDetail.CostDate;
                _originalCard.CostNotes = CardDetail.CostNotes;
                _originalCard.Quantity = CardDetail.Quantity;
                _originalCard.ListingType = CardDetail.ListingType;
                _originalCard.Offerable = CardDetail.Offerable;
                _originalCard.ShippingProfile = CardDetail.ShippingProfile;
                _originalCard.WhatnotCategory = CardDetail.WhatnotCategory;
                _originalCard.WhatnotSubcategory = CardDetail.WhatnotSubcategory;
                _originalCard.Notes = CardDetail.Notes;
                _originalCard.UpdatedAt = DateTime.UtcNow;

                await _cardRepository.UpdateCardAsync(_originalCard);

                _logger.LogInformation("Card {CardId} updated: {PlayerName}", _originalCard.Id, _originalCard.PlayerName);

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
