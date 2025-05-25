using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using l4d2addon_installer.Services;
using l4d2addon_installer.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace l4d2addon_installer.Views;

public partial class OperationPanelView : DataContextUserControl<OperationPanelViewModel>
{
    private readonly string[] _extensions = [".vpk", ".zip", ".rar"];

    public OperationPanelView()
    {
        InitializeComponent();
        
        if (Design.IsDesignMode) return;
        BottomPanel.SetValue(DragDrop.AllowDropProperty, true);
        BottomPanel.AddHandler(DragDrop.DropEvent, BottomPanel_Drop);
        BottomPanel.PointerPressed += BottomPanel_PointerPressed;
    }

    //选择文件安装
    private async void BottomPanel_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var storageProvider = TopLevel.GetTopLevel(this)!.StorageProvider;
        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择",
            AllowMultiple = true,
            FileTypeFilter = [new FilePickerFileType("mod文件") {Patterns = ["*.vpk", "*.zip", "*.rar"]}]
        });
        var filePaths = files.Select(f => f.Path.LocalPath).ToList();
        if (filePaths.Count == 0) return;

        await InstallVpkAsync(filePaths);
    }

    //拖动文件安装
    private async void BottomPanel_Drop(object? sender, DragEventArgs e)
    {
        if (!e.Data.Contains(DataFormats.Files)) return;

        var filePaths = e.Data.GetFiles()?.Select(f => f.Path.LocalPath).ToList();
        if (filePaths == null || filePaths.Count == 0) return;

        foreach (string path in filePaths)
        {
            //判断后缀名是否正确
            string extension = Path.GetExtension(path);
            bool result = _extensions.Any(t => t.Equals(extension, StringComparison.OrdinalIgnoreCase));
            if (!result)
            {
                //todo 弹窗提示
                return;
            }
        }

        await InstallVpkAsync(filePaths);
    }

    private async Task InstallVpkAsync(List<string> filePaths)
    {
        Debug.Assert(DataContext is not null);
        Debug.Assert(Provider is not null);

        DataContext.ShowLoading = true;
        var vpkFileService = Provider.GetRequiredService<VpkFileService>();
        try
        {
            await vpkFileService.InstallVpkFilesAsync(filePaths);
        }
        catch (AggregateException e)
        {
            var logger = Provider.GetService<LoggerService>();
            if (logger == null) throw;
            foreach (var innerException in e.InnerExceptions)
            {
                logger.LogError(innerException.Message);
            }
        }
        finally
        {
            DataContext.ShowLoading = false;
        }
    }
}
