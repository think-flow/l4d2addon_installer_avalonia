using System;
using System.Collections.ObjectModel;
using l4d2addon_installer.Services;
using Microsoft.Extensions.DependencyInjection;

namespace l4d2addon_installer.ViewModels;

public class LogsPanelViewModel : ServiceViewModelBase
{
    [Obsolete("专供设计器调用", true)]
    public LogsPanelViewModel() : base(null!)
    {
        var now = DateTime.Now;
        var rd = new Random();
        for (int i = 0; i < 23; i++)
        {
            int temp = rd.Next(0, 2);
            var level = temp == 0 ? LogMessageType.Message : LogMessageType.Error;
            Logs.Add(new LogsItemViewModel(level, $"1111111111111111111111111111111111111文件名已写入剪贴板超长超长超长超长超长超长超长{i}", now.AddSeconds(i).ToString("HH:mm:ss")));
        }
    }

    public LogsPanelViewModel(IServiceProvider provider)
        : base(provider)
    {
        var logger = provider.GetRequiredService<LoggerService>();
        //设置处理log的回调函数
        logger.HandleLog += OnLogHandler;
    }

    public ObservableCollection<LogsItemViewModel> Logs { get; } = new();

    //处理log消息
    private void OnLogHandler(LogMessage log)
    {
        //因为打印日志可能由其他线程调用，所以将其通过Dispatcher返回ui线程
        ExecuteIfOnUiThread(() =>
        {
            Logs.Add(new LogsItemViewModel(log.Type, log.Message, log.Time.ToString("HH:mm:ss")));
        });
    }
}

public class LogsItemViewModel : ViewModelBase
{
    public LogsItemViewModel(LogMessageType logType, string message, string time)
    {
        LogType = logType;
        Message = message;
        Time = time;
    }

    public string Time { get; }

    public string Message { get; }

    public LogMessageType LogType { get; }

    public bool IsMessage => LogType == LogMessageType.Message;
}
