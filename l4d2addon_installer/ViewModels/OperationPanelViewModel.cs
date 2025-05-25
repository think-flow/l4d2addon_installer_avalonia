using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace l4d2addon_installer.ViewModels;

public partial class OperationPanelViewModel : ServiceViewModelBase
{
    [ObservableProperty]
    private bool _isCoverd;

    [ObservableProperty]
    private bool _showLoading;

    [Obsolete("专供设计器调用", true)]
    public OperationPanelViewModel() : base(null!)
    {
    }

    public OperationPanelViewModel(IServiceProvider provider)
        : base(provider)
    {
    }
}
