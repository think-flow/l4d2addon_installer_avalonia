using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using l4d2addon_installer.Services;
using l4d2addon_installer.ViewModels;
using l4d2addon_installer.Views;
using Microsoft.Extensions.DependencyInjection;

namespace l4d2addon_installer;

public class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public static MainWindow MainWindow { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // 注册应用程序运行所需的所有服务
        var services = new ServiceCollection();
        services.AddCommonServices();

        // 从 collection 提供的 IServiceCollection 中创建包含服务的 ServiceProvider
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            // 必须在创建窗口之前加载配置文件
            // 确保窗口在加载过程中能获取配置信息
            LoadAppConfig();

            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.DataContext = new MainWindowViewModel();
            desktop.MainWindow = mainWindow;

            //注册程序退出事件
            desktop.Exit += DesktopOnExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    //加载配置文件
    private void LoadAppConfig()
    {
        var appConfigService = Services.GetRequiredService<IAppConfigService>();
        var logger = Services.GetRequiredService<LoggerService>();
        try
        {
            appConfigService.LoadConfig();
        }
        catch (ServiceException ex)
        {
            logger.LogMessage(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
        }
    }

    //程序退出时的处理
    private void DesktopOnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        var appConfigService = Services.GetRequiredService<IAppConfigService>();
        try
        {
            appConfigService.StoreConfig();
        }
        catch
        {
            // ignored
        }
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
