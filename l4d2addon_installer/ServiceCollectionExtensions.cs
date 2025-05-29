using System;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using l4d2addon_installer.Services;
using l4d2addon_installer.ViewModels;
using l4d2addon_installer.Views;
using Microsoft.Extensions.DependencyInjection;

namespace l4d2addon_installer;

public static class ServiceCollectionExtensions
{
    private static readonly object _serviceKey = new();

    /// <summary>
    /// 在此添加应用程序需要的依赖服务
    /// </summary>
    public static IServiceCollection AddCommonServices(this IServiceCollection services)
    {
        services.AddSingleton<MainWindow>(p => new MainWindow {Provider = p.GetRequiredService<IServiceProvider>()});
        services.AddTransient<MainWindowViewModel>();
        services.AddKeyedTransient<TopLevel>(_serviceKey, (p, _) => TopLevel.GetTopLevel(p.GetRequiredService<MainWindow>())!);
        services.AddTransient<IClipboard>(p => p.GetRequiredKeyedService<TopLevel>(_serviceKey).Clipboard!);

        services.AddSingleton<VpkFileService>();
        services.AddSingleton<LoggerService>();
        services.AddSingleton<IAppConfigService, YamlAppConfigService>();

        return services;
    }
}
