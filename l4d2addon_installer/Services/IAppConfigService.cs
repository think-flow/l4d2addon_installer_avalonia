using l4d2addon_installer.Models;

namespace l4d2addon_installer.Services;

public interface IAppConfigService
{
    /// <summary>
    /// 获取应用程序的配置信息
    /// </summary>
    AppConfig AppConfig { get; }

    /// <summary>
    /// 保存配置文件
    /// </summary>
    void StoreConfig();

    /// <summary>
    /// 加载配置文件
    /// </summary>
    void LoadConfig();
}
