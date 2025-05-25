using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using l4d2addon_installer.Services;
using l4d2addon_installer.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace l4d2addon_installer.Views;

public partial class VpkFilesPanelView : DataContextUserControl<VpkFilesPanelViewModel>
{
    private CancellationTokenSource _toolTipCancellation = new();

    public VpkFilesPanelView()
    {
        InitializeComponent();

        if (Design.IsDesignMode) return;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Debug.Assert(DataContext is not null);
        Debug.Assert(Provider is not null);

        var logger = Provider.GetRequiredService<LoggerService>();
        try
        {
            await DataContext.UpdateVpkFilesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
        }
    }

    private void TextBlock_OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is not TextBlock textBlock) return;

        ToolTip.SetIsOpen(textBlock, false);
        ToolTip.SetTip(textBlock, null);
        _toolTipCancellation.Cancel();
    }

    private void TextBlock_OnPointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is not TextBlock {DataContext: VpkFileInfoViewModel vm} textBlock) return;

        if (_toolTipCancellation.IsCancellationRequested)
        {
            _toolTipCancellation.Dispose();
            _toolTipCancellation = new CancellationTokenSource();
        }

        var cancelToken = _toolTipCancellation.Token;
        Task.Run(async () =>
        {
            await Task.Delay(1000, cancelToken);
            if (cancelToken.IsCancellationRequested) return;
            Dispatcher.UIThread.Post(() =>
            {
                if (cancelToken.IsCancellationRequested) return;

                //创建新的ToolTip
                var toolTip = CreateToolTip(vm);
                // 绑定到当前 TextBlock
                ToolTip.SetPlacement(textBlock, PlacementMode.Pointer);
                ToolTip.SetTip(textBlock, toolTip);
                ToolTip.SetIsOpen(textBlock, true);
            });
        }, cancelToken);
    }

    private ToolTip CreateToolTip(VpkFileInfoViewModel vm)
    {
        // 创建新的 ToolTip
        var toolTip = new ToolTip
        {
            Content = new StackPanel
            {
                Spacing = 2,
                Children =
                {
                    new TextBlock {Text = vm.Size},
                    new TextBlock {Text = vm.CreationTime}
                }
            },
            Classes = {"fileProperty"}
        };
        return toolTip;
    }
}
