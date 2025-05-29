using System;
using System.IO;
using System.Text;
using System.Threading;
using l4d2addon_installer.Models;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace l4d2addon_installer.Services;

public class YamlAppConfigService : IAppConfigService
{
    private const string ConfigFilePath = "./config.yaml";
    private readonly IDeserializer _deserializer = new StaticDeserializerBuilder(new MyYamlSerializerContext()).Build();
    private readonly ReaderWriterLockSlim _rwLock = new(LockRecursionPolicy.NoRecursion);
    private readonly ISerializer _serializer = new StaticSerializerBuilder(new MyYamlSerializerContext()).Build();
    private AppConfig? _config;

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
            using var fileStream = new FileStream(ConfigFilePath, FileMode.Create, FileAccess.Write);
            string yamlStr = _serializer.Serialize(AppConfig);
            byte[] buffer = Encoding.UTF8.GetBytes(yamlStr);
            fileStream.Write(buffer, 0, buffer.Length);
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
        TextReader? textReader = null;
        try
        {
            fileStream = new FileStream(ConfigFilePath, FileMode.Open, FileAccess.Read);
            textReader = new StreamReader(fileStream);
            string yamlStr = textReader.ReadToEnd();
            var config = _deserializer.Deserialize<AppConfig>(yamlStr);

            // 当文件内容为空时，config会为null
            if (config is null)
            {
                AppConfig = new AppConfig();
                throw new ServiceException("The content of the config.yaml file is empty. Will use the default configuration.");
            }

            AppConfig = config;
        }
        catch (FileNotFoundException e)
        {
            AppConfig = new AppConfig();
            throw new ServiceException("config.yaml file was not found. Will use the default configuration.", e);
        }
        catch (YamlException e)
        {
            AppConfig = new AppConfig();
            throw new ServiceException("The format of the config.yaml file is incorrect. Will use the default configuration.", e);
        }
        catch (InvalidCastException e)
        {
            AppConfig = new AppConfig();
            throw new ServiceException("The format of the config.yaml file is incorrect. Will use the default configuration.", e);
        }
        finally
        {
            textReader?.Dispose();
            fileStream?.Dispose();
        }
    }
}
