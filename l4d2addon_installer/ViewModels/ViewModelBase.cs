using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace l4d2addon_installer.ViewModels;

public class ViewModelBase : ObservableObject
{
    protected void ExecuteIfOnUiThread(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
        }
        else
        {
            Dispatcher.UIThread.Post(action);
        }
    }
}

public class ServiceViewModelBase(IServiceProvider provider) : ViewModelBase
{
    public IServiceProvider Provider { get; } = provider;
}
