using l4d2addon_installer.Services;
using Microsoft.Extensions.DependencyInjection;

namespace l4d2addon_installer;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 在此添加应用程序需要的依赖服务
    /// </summary>
    public static IServiceCollection AddCommonServices(this IServiceCollection services)
    {
        services.AddSingleton<VpkFileService>();
        services.AddSingleton<LoggerService>();
        services.AddSingleton<IAppConfigService, YamlAppConfigService>();

        return services;
    }
}
