using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Reactive;
using l4d2addon_installer.Services;
using l4d2addon_installer.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace l4d2addon_installer.Views;

public partial class MainWindow : Window
{
    private const double MinPercentageBase = 0.1;
    private const double MaxPercentageBase = 0.7;
    private bool _isLoaded;

    //记录窗口状态变化的前一个状态
    private WindowState _preWindowState = WindowState.Normal;

    public MainWindow()
    {
        InitializeComponent();

#if DEBUG
        if (Design.IsDesignMode) return;
#endif

        SizeChanged += OnSizeChanged;
        Loaded += OnLoaded;
        Closing += OnClosing;
        this.GetObservable(WindowStateProperty).Subscribe(new AnonymousObserver<WindowState>(OnStateChanged));
    }

    private IServiceProvider Services => App.Services;

    //监听窗口的状态
    private void OnStateChanged(WindowState state)
    {
        Log.Debug("MainWindow WindowState: {@WindowState}", state);
        if (OperatingSystem.IsWindows())
        {
            if (state == WindowState.Minimized)
            {
                // 最小化窗口时，启用EcoQos
                EcoQosProcess.EnableEcoQos();
            }
            else if (_preWindowState == WindowState.Minimized)
            {
                // 当由最小化变为其他状态时，禁用EcoQos
                EcoQosProcess.DisableEcoQos();
            }
        }

        _preWindowState = state;
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        var grid = Container;
        var leftCol = grid.ColumnDefinitions[0];
        var appConfig = Services.GetRequiredService<IAppConfigService>().AppConfig;
        appConfig.LeftColWidthPercent = leftCol.ActualWidth / Width;

        var dataContext = (MainWindowViewModel) DataContext!;
        appConfig.IsCoverd = dataContext.IsCoverd;
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
        leftCol.MinWidth = newWindowWidth * MinPercentageBase;
        leftCol.MaxWidth = newWindowWidth * MaxPercentageBase;
    }

    //初始化左边框宽度
    private void InitializeLeftColWidth()
    {
        var appConfig = Services.GetRequiredService<IAppConfigService>().AppConfig;

        //如果没有配置LeftColWidthPercent，则回滚回编译时宽度
        if (appConfig.LeftColWidthPercent is null) return;

        double percent = appConfig.LeftColWidthPercent.Value;
        var grid = Container;
        var leftCol = grid.ColumnDefinitions[0];

        if (percent < MinPercentageBase)
        {
            percent = MinPercentageBase;
        }
        else if (percent > MaxPercentageBase)
        {
            percent = MaxPercentageBase;
        }

        leftCol.Width = new GridLength(Width * percent, GridUnitType.Pixel);
    }
}
