using System;
using Avalonia.Controls;
using l4d2addon_installer.ViewModels;

namespace l4d2addon_installer.Views;

public abstract class DataContextUserControl<TDataContext> : UserControl
    where TDataContext : ViewModelBase
{
    protected IServiceProvider Services => App.Services;

    protected new TDataContext DataContext
    {
        get => (TDataContext) base.DataContext!;
        init => base.DataContext = value;
    }
}
