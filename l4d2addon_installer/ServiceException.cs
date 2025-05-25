using System;

namespace l4d2addon_installer;

/// <summary>
/// 自定义service异常，当service中出现异常时，返回的类型
/// </summary>
public class ServiceException : Exception
{
    public ServiceException(string? message)
        : base(message)
    {
    }

    public ServiceException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
