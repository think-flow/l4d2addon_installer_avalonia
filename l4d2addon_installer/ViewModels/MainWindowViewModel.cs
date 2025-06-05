using CommunityToolkit.Mvvm.ComponentModel;
using l4d2addon_installer.Services;
using Microsoft.Extensions.DependencyInjection;

namespace l4d2addon_installer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isCoverd;

    public MainWindowViewModel()
    {
#if DEBUG
        if (IsDesignMode) return;
#endif

        InitializeIsCoverd();
    }

    private void InitializeIsCoverd()
    {
        var appConfig = Services.GetRequiredService<IAppConfigService>().AppConfig;
        bool isCoverd = appConfig.IsCoverd ?? false;
        IsCoverd = isCoverd;
    }
}
