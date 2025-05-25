using System;
using Avalonia.Controls;
using l4d2addon_installer.ViewModels;

namespace l4d2addon_installer.Views;

public class DataContextUserControl<TDataContext> : UserControl
    where TDataContext : ServiceViewModelBase
{
    protected DataContextUserControl()
    {
        DataContextChanged += OnDataContextChanged;
    }

    protected IServiceProvider? Provider { get; private set; }

    protected new TDataContext? DataContext { get; private set; }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        var dataContext = (TDataContext) base.DataContext!;
        DataContext = dataContext;
        Provider = dataContext.Provider;
    }
}
