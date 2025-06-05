using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace l4d2addon_installer.ViewModels;

public class ViewModelBase : ObservableObject
{
    protected IServiceProvider Services => App.Services;

    protected bool IsDesignMode => Design.IsDesignMode;

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

    protected Task ExecuteIfOnUiThread(Func<Task> action) => Dispatcher.UIThread.CheckAccess() ? action() : Dispatcher.UIThread.InvokeAsync(action);
}
