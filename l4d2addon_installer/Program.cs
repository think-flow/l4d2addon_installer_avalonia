using System;
using System.Runtime.InteropServices;
using Avalonia;

namespace l4d2addon_installer;

internal sealed partial class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            //在此捕获全局异常
            NativeMessageBox.ShowError(e.ToString(), "Error");
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

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
