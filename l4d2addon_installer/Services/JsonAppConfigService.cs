using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using l4d2addon_installer.Json.Context;
using l4d2addon_installer.Models;

namespace l4d2addon_installer.Services;

public class JsonAppConfigService : IAppConfigService
{
    private readonly ReaderWriterLockSlim _rwLock = new(LockRecursionPolicy.NoRecursion);
    private AppConfig? _config;

    // private JsonSerializerOptions DefaultSerializerOption { get; } = new()
    // {
    //     //该Resolver 表示使用基于反射的序列化和反序列化
    //     TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    // };

    /// <summary>
    /// 获取应用程序的配置信息
    /// </summary>
    /// <exception cref="InvalidOperationException">AppConfig is not loaded! Please call LoadConfig()</exception>
    public AppConfig AppConfig
    {
        get
        {
            _rwLock.EnterReadLock();
            try
            {
                if (_config == null) throw new InvalidOperationException("AppConfig is not loaded! Please call LoadConfig()");
                return _config;
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
        }
        private set
        {
            _rwLock.EnterWriteLock();
            try
            {
                _config = value;
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }
    }

    /// <summary>
    /// 保存配置文件
    /// </summary>
    /// <exception cref="ServiceException">FileNotFound and other exception</exception>
    public void StoreConfig()
    {
        try
        {
            using var fileStream = new FileStream("./config.json", FileMode.Create, FileAccess.Write);

            // 采用反射模式
            // JsonSerializer.Serialize(fileStream, AppConfig, DefaultSerializerOption);
            // 采用源生成器模式
            JsonSerializer.Serialize(fileStream, AppConfig, MyJsonSerializerContext.Default.AppConfig);
        }
        catch (Exception e)
        {
            throw new ServiceException(e.Message, e);
        }
    }

    /// <summary>
    /// 加载配置文件
    /// </summary>
    /// <exception cref="ServiceException">FileNotFound and format and other exception</exception>
    /// <exception cref="Exception"></exception>
    public void LoadConfig()
    {
        FileStream? fileStream = null;
        try
        {
            fileStream = new FileStream("./config.json", FileMode.Open, FileAccess.Read);
            // 采用反射模式
            // var config = JsonSerializer.Deserialize<AppConfig>(fileStream, DefaultSerializerOption);
            // 采用源生成器模式
            var config = JsonSerializer.Deserialize(fileStream, MyJsonSerializerContext.Default.AppConfig);
            AppConfig = config ?? throw new JsonException();
        }
        catch (FileNotFoundException e)
        {
            AppConfig = new AppConfig();
            throw new ServiceException("config.json file was not found. Will use the default configuration.", e);
        }
        catch (JsonException e)
        {
            AppConfig = new AppConfig();
            throw new ServiceException("The format of the config.json file is incorrect. Will use the default configuration.", e);
        }
        finally
        {
            fileStream?.Dispose();
        }
    }
}
