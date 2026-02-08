using CommunityToolkit.Mvvm.ComponentModel;
using CardLister.Core.Models;

namespace CardLister.Desktop.Models
{
    public partial class SelectableCard : ObservableObject
    {
        public Card Card { get; }

        [ObservableProperty] private bool _isSelected;

        public SelectableCard(Card card)
        {
            Card = card;
        }
    }
}
