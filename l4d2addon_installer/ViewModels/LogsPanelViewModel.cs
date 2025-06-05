using System;
using System.Collections.ObjectModel;
using System.Threading.Channels;
using System.Threading.Tasks;
using l4d2addon_installer.Services;
using Microsoft.Extensions.DependencyInjection;

namespace l4d2addon_installer.ViewModels;

public class LogsPanelViewModel : ViewModelBase
{
    public LogsPanelViewModel()
    {
#if DEBUG
        if (IsDesignMode)
        {
            var now = DateTime.Now;
            var rd = new Random();
            for (int i = 0; i < 23; i++)
            {
                int temp = rd.Next(0, 2);
                var level = temp == 0 ? LogMessage.MessageType.Message : LogMessage.MessageType.Error;
                Logs.Add(new LogsItemViewModel(level, $"1111111111111111111111111111111111111文件名已写入剪贴板超长超长超长超长超长超长超长{i}", now.AddSeconds(i).ToString("HH:mm:ss")));
            }

            return;
        }
#endif
        //设置处理log消息的函数
        var logger = Services.GetRequiredService<LoggerService>();
        HandleLogMessage(logger.LogMessageReader);
    }

    public ObservableCollection<LogsItemViewModel> Logs { get; } = new();

    //处理log消息
    private async void HandleLogMessage(ChannelReader<LogMessage> reader)
    {
        await foreach (var msg in reader.ReadAllAsync().ConfigureAwait(false))
        {
            //因为打印日志可能由其他线程调用，所以将其通过Dispatcher返回ui线程
            ExecuteIfOnUiThread(() =>
            {
                Logs.Add(new LogsItemViewModel(msg.Type, msg.Message, msg.Time.ToString("HH:mm:ss")));
            });
        }
    }
}

public class LogsItemViewModel : ViewModelBase
{
    public LogsItemViewModel(LogMessage.MessageType type, string message, string time)
    {
        Type = type;
        Message = message;
        Time = time;
    }

    public string Time { get; }

    public string Message { get; }

    public LogMessage.MessageType Type { get; }

    public bool IsMessage => Type == LogMessage.MessageType.Message;
}
