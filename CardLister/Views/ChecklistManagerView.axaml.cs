using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CardLister.Desktop.Views
{
    public partial class ChecklistManagerView : UserControl
    {
        public ChecklistManagerView()
        {
            InitializeComponent();
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (DataContext is CardLister.Desktop.ViewModels.ChecklistManagerViewModel vm)
                vm.LoadCommand.Execute(null);
        }
    }
}
