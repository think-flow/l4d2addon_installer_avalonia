using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using l4d2addon_installer.Collections.ObjectModel;
using l4d2addon_installer.Models;
using l4d2addon_installer.Services;
using l4d2addon_installer.Views;
using Microsoft.Extensions.DependencyInjection;

namespace l4d2addon_installer.ViewModels;

public partial class VpkFilesPanelViewModel : ViewModelBase
{
    private readonly LoggerService _logger = null!;
    private readonly VpkFileService _vpkFileService = null!;

    [ObservableProperty]
    private bool _canRevealFile;

    public VpkFilesPanelViewModel()
    {
#if DEBUG
        if (IsDesignMode)
        {
            var now = DateTime.Now;
            for (int i = 0; i < 23; i++)
            {
                VpkFiles.Add(new VpkFileInfoViewModel(new VpkFileInfo
                {
                    CreationTime = now.AddHours(1),
                    ModifiedTime = now.AddHours(1),
                    FullPath = $"哈哈dds/kjlkjj/{i}.vpk",
                    Name = $"哈哈ddd{i}.vpk",
                    NameWithoutEx = "kjljkkjwekjlr",
                    Size = 1111132
                }));
            }

            return;
        }
#endif

        _vpkFileService = Services.GetRequiredService<VpkFileService>();
        _logger = Services.GetRequiredService<LoggerService>();

        // 监听选中项变化
        SelectedVpkFiles = new ObservableCollection<VpkFileInfoViewModel>();
        SelectedVpkFiles.CollectionChanged += (_, _) =>
        {
            CanRevealFile = SelectedVpkFiles.Count == 1;
        };

        //设置处理addons文件夹变更的回调函数
        StartFileWatcherMessageHandler(_vpkFileService.FileWatcherMessageReader);
    }

    public ObservableSortedCollection<VpkFileInfoViewModel> VpkFiles { get; } = new(new VpkFileInfoViewModel.VpkFileInfoViewModelComparer());

    public ObservableCollection<VpkFileInfoViewModel> SelectedVpkFiles { get; } = null!;

    private bool CanCopyFileName => SelectedVpkFiles.Count > 0;

    private bool CanDeleteFile => SelectedVpkFiles.Count > 0;

    [RelayCommand(CanExecute = nameof(CanCopyFileName))]
    private async Task CopyFileName()
    {
        string fileNames = string.Join(' ', SelectedVpkFiles.Select(p => $"\"{p.Name}\""));

        await TopLevel.GetTopLevel(App.MainWindow)!.Clipboard!.SetTextAsync(fileNames);
        _logger.LogMessage("文件名已写入剪贴板");
    }

    [RelayCommand(CanExecute = nameof(CanRevealFile))]
    private async Task RevealFile(VpkFileInfoViewModel fileInfo)
    {
        try
        {
            await _vpkFileService.RevealFileAsync(fileInfo);
        }
        catch (ServiceException e)
        {
            _logger.LogError(e.Message);
        }
    }

    [RelayCommand(CanExecute = nameof(CanDeleteFile))]
    private async Task DeleteFile()
    {
        if (SelectedVpkFiles.Count <= 0) return;

        bool toTrash = true;
        bool isSuccessd = true;
        await _vpkFileService.DeleteVpkFilesAsync(SelectedVpkFiles.ToArray(), toTrash, (msg, ex) =>
        {
            if (ex is not null)
            {
                Message.Success("删除失败");
                foreach (var inner in ex.InnerExceptions)
                {
                    _logger.LogError(inner.Message);
                }

                return;
            }

            if (msg is not null)
            {
                _logger.LogMessage(msg);
            }
        });
        if (isSuccessd)
        {
            Message.Success("删除成功");
        }
        else
        {
            Message.Error("删除失败，请查看日志");
        }
    }

    /// <summary>
    /// 更新vpk列表
    /// </summary>
    public async Task UpdateVpkFilesAsync()
    {
        try
        {
            var vpkFileInfos = await _vpkFileService.GetVpkFilesAsync();

            ExecuteIfOnUiThread(() =>
            {
                VpkFiles.Clear();

                foreach (var vpkFileInfo in vpkFileInfos)
                {
                    VpkFiles.Add(new VpkFileInfoViewModel(vpkFileInfo));
                }
            });
        }
        catch (ServiceException e)
        {
            _logger.LogError(e.Message);
        }
    }

    private void StartFileWatcherMessageHandler(ChannelReader<FileWatcherMessage> reader)
    {
        //启动一个专用线程，用来监听文件夹变化
        var thread = new Thread(() =>
        {
            while (reader.WaitToReadAsync().Preserve().GetAwaiter().GetResult())
            {
                var msg = reader.ReadAsync().Preserve().GetAwaiter().GetResult();
                _ = ExecuteIfOnUiThread(async () =>
                {
                    switch (msg.Type)
                    {
                        case FileWatcherMessage.MessageType.Created:
                            await OnVpkFileCreatedHandlerAsync(msg.FilePath);
                            break;
                        case FileWatcherMessage.MessageType.Deleted:
                            await OnVpkFileDeletedHandlerAsync(msg.FilePath);
                            break;
                        case FileWatcherMessage.MessageType.Renamed:
                            await OnVpkFileRenamedHandlerAsync(msg.OldFilePath, msg.FilePath);
                            break;
                        default:
                            throw new InvalidOperationException("invalid message type");
                    }
                });
            }
        })
        {
            Name = "VpkFileWatcherThread",
            IsBackground = true
        };
        thread.Start();
    }

    //处理addons文件夹删除了vpk文件，通知
    private async Task OnVpkFileCreatedHandlerAsync(string fullPath)
    {
        try
        {
            var fileInfo = await _vpkFileService.GetVpkFileAsync(fullPath);
            VpkFiles.Add(new VpkFileInfoViewModel(fileInfo));
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
        }
    }

    //处理addons文件夹删除了vpk文件，通知
    private Task OnVpkFileDeletedHandlerAsync(string fullPath)
    {
        VpkFiles.RemoveBasedEquals(new VpkFileInfoViewModel {Path = fullPath});
        return Task.CompletedTask;
    }

    //处理addons文件夹删除了vpk文件，通知
    private async Task OnVpkFileRenamedHandlerAsync(string oldFullPath, string newFullPath)
    {
        string oldEx = Path.GetExtension(oldFullPath);
        string newEx = Path.GetExtension(newFullPath);
        if (string.Equals(oldEx, newEx, StringComparison.OrdinalIgnoreCase))
        {
            //重命名   删除然后新增
            await OnVpkFileDeletedHandlerAsync(oldFullPath);
            await OnVpkFileCreatedHandlerAsync(newFullPath);
        }
        else if (!oldEx.Equals(".vpk", StringComparison.OrdinalIgnoreCase))
        {
            //其他扩展名改成vpk了  新增
            await OnVpkFileCreatedHandlerAsync(newFullPath);
        }
        else
        {
            //vpk改成其他扩展名了   删除
            await OnVpkFileDeletedHandlerAsync(oldFullPath);
        }
    }
}

public partial class VpkFileInfoViewModel : ViewModelBase
{
    private static readonly Dictionary<string, double> _thresholds = new()
    {
        {"TB", Math.Pow(1024, 4)},
        {"GB", Math.Pow(1024, 3)},
        {"MB", Math.Pow(1024, 2)},
        {"KB", 1024},
        {"B", 1}
    };

    private readonly DateTimeOffset _creationTimeCore;

    /// <summary>
    /// 文件创建日期
    /// </summary>
    [ObservableProperty]
    private string _creationTime = string.Empty;

    /// <summary>
    /// 文件名称（带扩展名）
    /// </summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>
    /// 文件名称（不带扩展名）
    /// </summary>
    [ObservableProperty]
    private string _nameWithoutEx = string.Empty;

    /// <summary>
    /// 文件全路径
    /// </summary>
    [ObservableProperty]
    private string _path = string.Empty;

    /// <summary>
    /// 文件大小
    /// </summary>
    [ObservableProperty]
    private string _size = string.Empty;

    public VpkFileInfoViewModel()
    {
    }

    public VpkFileInfoViewModel(VpkFileInfo fileInfo)
    {
        Name = fileInfo.Name;
        NameWithoutEx = fileInfo.NameWithoutEx;
        Path = fileInfo.FullPath;
        _creationTimeCore = fileInfo.CreationTime;
        CreationTime = string.Concat("创建日期: ", fileInfo.CreationTime.ToString("yyyy-MM-dd HH:mm"));
        Size = string.Concat("大小: ", FormatFileSize(fileInfo.Size));
    }

    private static string FormatFileSize(long size)
    {
        if (size == 0) return "0 B";
        string unit = "B";
        foreach (string u in _thresholds.Keys)
        {
            if (size >= _thresholds[u])
            {
                unit = u;
                break;
            }
        }

        return string.Concat(Math.Floor(size / _thresholds[unit] * 100) / 100, " ", unit);
    }

    internal class VpkFileInfoViewModelComparer : IComparer<VpkFileInfoViewModel>
    {
        public int Compare(VpkFileInfoViewModel? x, VpkFileInfoViewModel? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (y is null) return 1;
            if (x is null) return -1;

            if (x.Path == y.Path) return 0;

            //根据创建日期从大到小排序
            int creationTimeComparison = y._creationTimeCore.CompareTo(x._creationTimeCore);
            if (creationTimeComparison != 0) return creationTimeComparison;

            //如果创建日期相同则通过名称从小到大排序
            return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
        }
    }
}
