using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using l4d2addon_installer.Services;
using Microsoft.Extensions.DependencyInjection;

namespace l4d2addon_installer.Views;

public partial class MainWindow : Window
{
    private const double MIN_PERCENTAGE_BASE = 0.1;
    private const double MAX_PERCENTAGE_BASE = 0.7;
    private bool _isLoaded;

    public MainWindow()
    {
        InitializeComponent();

        if (Design.IsDesignMode) return;
        SizeChanged += OnSizeChanged;
        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    public IServiceProvider Provider { get; init; } = null!;

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        var grid = Container;
        var leftCol = grid.ColumnDefinitions[0];
        var appConfig = Provider.GetRequiredService<AppConfigService>().AppConfig;
        appConfig.LeftColWidthPercent = leftCol.ActualWidth / Width;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _isLoaded = true;
        InitializeLeftColWidth();
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        SetGridRangeWidth(e);
    }

    //当窗口大小改变时，重新设置grid第一列的最大和最小宽度
    private void SetGridRangeWidth(SizeChangedEventArgs e)
    {
        var grid = Container;
        var leftCol = grid.ColumnDefinitions[0];
        double prevWindowWidth = e.PreviousSize.Width;
        double newWindowWidth = e.NewSize.Width;

        if (_isLoaded)
        {
            //只有当窗口通过用户操作改变大小时，才执行这里的逻辑

            //计算先前的leftCol占比
            double percent = leftCol.ActualWidth / prevWindowWidth;
            //根据比例设置新的width
            double newLeftColWidth = newWindowWidth * percent;
            leftCol.Width = new GridLength(newLeftColWidth, GridUnitType.Pixel);
        }

        //限制left最大和最小范围 10%-70%
        leftCol.MinWidth = newWindowWidth * MIN_PERCENTAGE_BASE;
        leftCol.MaxWidth = newWindowWidth * MAX_PERCENTAGE_BASE;
    }

    //初始化左边框宽度
    private void InitializeLeftColWidth()
    {
        var appConfig = Provider.GetRequiredService<AppConfigService>().AppConfig;
        double percent = appConfig.LeftColWidthPercent ?? 0;
        var grid = Container;
        var leftCol = grid.ColumnDefinitions[0];

        if (percent < MIN_PERCENTAGE_BASE)
        {
            percent = MIN_PERCENTAGE_BASE;
        }
        else if (percent > MAX_PERCENTAGE_BASE)
        {
            percent = MAX_PERCENTAGE_BASE;
        }

        leftCol.Width = new GridLength(Width * percent, GridUnitType.Pixel);
    }
}
