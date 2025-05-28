using System;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia;
using Serilog;
using Serilog.Events;

namespace l4d2addon_installer;

internal sealed partial class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        //初始化Serilog
        ConfigureLogger();
        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            //在此捕获全局异常
#if DEBUG
            Log.Debug(e, "未处理异常");
            NativeMessageBox.ShowError(e.ToString(), "Error");
#else
            Log.Fatal(e,"未处理异常");
            NativeMessageBox.ShowError("未处理异常", "Error");
#endif
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect();

    private static void ConfigureLogger()
    {
        string logFilePath = Path.Combine(AppContext.BaseDirectory, "log.txt");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
#if DEBUG
            .WriteTo.Debug(
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level}] {Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel: LogEventLevel.Debug
            )
#endif
            .WriteTo.File(
                logFilePath,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level}] {Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel: LogEventLevel.Error // 文件仅记录 Error 及以上
            )
            .CreateLogger();
    }

    private static partial class NativeMessageBox
    {
        [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
        private static partial int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

        public static void ShowError(string message, string caption)
        {
            _ = MessageBoxW(IntPtr.Zero, message, caption, 0x00000010);
        }
    }
}
