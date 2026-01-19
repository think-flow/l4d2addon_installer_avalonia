using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
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
        DataContext = new OperationPanelViewModel();
#if DEBUG
        if (Design.IsDesignMode) return;
#endif
    }

    //选择文件安装
    // ReSharper disable once AsyncVoidMethod
    private async void BottomPanel_OnPointerPressed(object? _, PointerPressedEventArgs e)
    {
        e.Handled = true;
        if (DataContext.ShowLoading) return;
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
    // ReSharper disable once AsyncVoidMethod
    // ReSharper disable once UnusedMember.Local
    private async void BottomPanel_OnDrop(object? _, DragEventArgs e)
    {
        e.Handled = true;
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
                Message.Error($"只支持 {_extensions.Aggregate((pv, cv) => pv + ' ' + cv)} 文件");
                return;
            }
        }

        await InstallVpkAsync(filePaths);
    }

    private async Task InstallVpkAsync(List<string> filePaths)
    {
        var logger = Services.GetRequiredService<LoggerService>();
        DataContext.ShowLoading = true;

        bool isCoverd = IsCoverd;
        var vpkFileService = Services.GetRequiredService<VpkFileService>();

        bool isSucceed = true;
        //并行安装文件
        await vpkFileService.InstallVpkFilesAsync(filePaths, isCoverd, (msg, ex) =>
        {
            if (ex is not null)
            {
                isSucceed = false;
                foreach (var inner in ex.InnerExceptions)
                {
                    logger.LogError(inner.Message);
                }

                return;
            }

            if (msg is not null)
            {
                logger.LogMessage(msg);
            }
        });
        if (isSucceed)
        {
            Message.Success("安装成功");
        }
        else
        {
            Message.Error("安装失败，请查看日志");
        }

        DataContext.ShowLoading = false;
    }

    #region Dependency Properties

    private bool _isCoverd;

    public static readonly DirectProperty<OperationPanelView, bool> IsCoverdProperty = AvaloniaProperty.RegisterDirect<OperationPanelView, bool>(
        nameof(IsCoverd),
        o => o.IsCoverd,
        (o, v) => o.IsCoverd = v,
        defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    /// 指示是否启用替换文件功能
    /// </summary>
    public bool IsCoverd
    {
        get => _isCoverd;
        set => SetAndRaise(IsCoverdProperty, ref _isCoverd, value);
    }

    #endregion
}
