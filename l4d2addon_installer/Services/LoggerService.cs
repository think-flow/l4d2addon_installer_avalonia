using System;

namespace l4d2addon_installer.Services;

public class LoggerService
{
    public Action<LogMessage>? HandleLog { get; set; }

    public void LogMessage(string message) => HandleLog?.Invoke(new LogMessage(message, LogMessageType.Message));

    public void LogError(string message) => HandleLog?.Invoke(new LogMessage(message, LogMessageType.Error));
}

public class LogMessage
{
    public LogMessage(string message, LogMessageType type)
    {
        Type = type;
        Message = message;
        Time = DateTimeOffset.Now;
    }

    public LogMessageType Type { get; }

    public DateTimeOffset Time { get; }

    public string Message { get; }
}

public enum LogMessageType
{
    Message,
    Error
}
