using System;
using System.Threading.Channels;
using Serilog;

namespace l4d2addon_installer.Services;

public class LoggerService
{
    private readonly Channel<LogMessage> _channel = Channel.CreateUnbounded<LogMessage>();

    public ChannelReader<LogMessage> LogMessageReader => _channel.Reader;

    public void LogMessage(string message)
    {
        Log.Information("{msg}", message);
        _channel.Writer.TryWrite(new LogMessage(message, Services.LogMessage.MessageType.Message));
    }

    public void LogError(string message)
    {
        Log.Information("{msg}", message);
        _channel.Writer.TryWrite(new LogMessage(message, Services.LogMessage.MessageType.Message));
    }
}

public class LogMessage
{
    public enum MessageType
    {
        Message,
        Error
    }

    public LogMessage(string message, MessageType type)
    {
        Type = type;
        Message = message;
        Time = DateTimeOffset.Now;
    }

    public MessageType Type { get; }

    public DateTimeOffset Time { get; }

    public string Message { get; }
}
