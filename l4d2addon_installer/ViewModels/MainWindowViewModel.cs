using CommunityToolkit.Mvvm.ComponentModel;

namespace l4d2addon_installer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isCoverd;
}
