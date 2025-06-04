using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using l4d2addon_installer.ViewModels;

namespace l4d2addon_installer.Views;

public partial class LogsPanelView : DataContextUserControl<LogsPanelViewModel>
{
    public LogsPanelView()
    {
        InitializeComponent();
        DataContext = new LogsPanelViewModel();
        if (Design.IsDesignMode) return;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Debug.Assert(DataContext is not null);
        DataContext.Logs.CollectionChanged += OnLogsCollectionChanged;
    }

    private async void OnLogsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        await Task.Delay(100);
        //将日志框滚动到最底部
        LogScrollViewer.ScrollToEnd();
    }
}
