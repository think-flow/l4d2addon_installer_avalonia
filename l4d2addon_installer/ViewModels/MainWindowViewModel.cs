using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace l4d2addon_installer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private LogsPanelViewModel _logsPanelViewModel;

    [ObservableProperty]
    private OperationPanelViewModel _operationPanelViewModel;

    [ObservableProperty]
    private VpkFilesPanelViewModel _vpkFileInfoViewModel;

    [Obsolete("专供设计器调用", true)]
    public MainWindowViewModel()
    {
        _operationPanelViewModel = new OperationPanelViewModel {ShowLoading = false};
    }

    public MainWindowViewModel(IServiceProvider provider)
    {
        _operationPanelViewModel = new OperationPanelViewModel(provider);
        _logsPanelViewModel = new LogsPanelViewModel(provider);
        _vpkFileInfoViewModel = new VpkFilesPanelViewModel(provider);
    }
}
