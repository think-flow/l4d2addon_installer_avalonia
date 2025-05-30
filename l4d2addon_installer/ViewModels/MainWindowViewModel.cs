namespace l4d2addon_installer.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel()
    {
        OperationPanelViewModel = new OperationPanelViewModel();
        LogsPanelViewModel = new LogsPanelViewModel();
        VpkFileInfoViewModel = new VpkFilesPanelViewModel();
    }

    public LogsPanelViewModel LogsPanelViewModel { get; }

    public OperationPanelViewModel OperationPanelViewModel { get; }

    public VpkFilesPanelViewModel VpkFileInfoViewModel { get; }
}
