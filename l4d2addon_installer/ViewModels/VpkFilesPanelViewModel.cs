using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using l4d2addon_installer.Collections.ObjectModel;
using l4d2addon_installer.Models;
using l4d2addon_installer.Services;
using Microsoft.Extensions.DependencyInjection;

namespace l4d2addon_installer.ViewModels;

public partial class VpkFilesPanelViewModel : ServiceViewModelBase
{
    private readonly IClipboard _clipboard;
    private readonly LoggerService _logger;
    private readonly VpkFileService _vpkFileService;

    [ObservableProperty]
    private bool _canRevealFile;

    [Obsolete("专供设计器调用", true)]
    public VpkFilesPanelViewModel() : base(null!)
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
    }

    public VpkFilesPanelViewModel(IServiceProvider provider)
        : base(provider)
    {
        _vpkFileService = provider.GetRequiredService<VpkFileService>();
        _logger = provider.GetRequiredService<LoggerService>();
        _clipboard = provider.GetRequiredService<IClipboard>();

        // 监听选中项变化
        SelectedVpkFiles = new ObservableCollection<VpkFileInfoViewModel>();
        SelectedVpkFiles.CollectionChanged += (_, _) =>
        {
            CanRevealFile = SelectedVpkFiles.Count == 1;
        };

        //设置处理addons文件夹变更的回调函数
        _vpkFileService.OnVpkFileCreatedHandler = OnVpkFileCreatedHandler;
        _vpkFileService.OnVpkFileDeletedHandler = OnVpkFileDeletedHandler;
        _vpkFileService.OnVpkFileRenamedHandler = OnVpkFileRenamedHandler;
    }

    public ObservableSortedCollection<VpkFileInfoViewModel> VpkFiles { get; } = new(new VpkFileInfoViewModel.VpkFileInfoViewModelComparer());

    public ObservableCollection<VpkFileInfoViewModel> SelectedVpkFiles { get; }

    private bool CanCopyFileName => SelectedVpkFiles.Count > 0;

    private bool CanDeleteFile => SelectedVpkFiles.Count > 0;

    [RelayCommand(CanExecute = nameof(CanCopyFileName))]
    private async Task CopyFileName()
    {
        string fileNames = string.Join(' ', SelectedVpkFiles.Select(p => $"\"{p.Name}\""));
        await _clipboard.SetTextAsync(fileNames);
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

        try
        {
            await _vpkFileService.DeleteVpkFilesAsync(SelectedVpkFiles.ToArray(), true);
        }
        catch (AggregateException e)
        {
            foreach (var innerException in e.InnerExceptions)
            {
                _logger.LogError(innerException.Message);
            }
        }
    }

    /// <summary>
    /// 更新vpk列表
    /// </summary>
    public virtual async Task UpdateVpkFilesAsync()
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

    //处理addons文件夹删除了vpk文件，通知
    private async void OnVpkFileCreatedHandler(string fullPath)
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
    private void OnVpkFileDeletedHandler(string fullPath)
    {
        VpkFiles.RemoveBasedEquals(new VpkFileInfoViewModel {Path = fullPath});
    }

    //处理addons文件夹删除了vpk文件，通知
    private void OnVpkFileRenamedHandler(string oldFullPath, string newFullPath)
    {
        string oldEx = Path.GetExtension(oldFullPath);
        string newEx = Path.GetExtension(newFullPath);
        if (string.Equals(oldEx, newEx, StringComparison.OrdinalIgnoreCase))
        {
            //重命名   删除然后新增
            OnVpkFileDeletedHandler(oldFullPath);
            OnVpkFileCreatedHandler(newFullPath);
        }
        else if (!oldEx.Equals(".vpk", StringComparison.OrdinalIgnoreCase))
        {
            //其他扩展名改成vpk了  新增
            OnVpkFileCreatedHandler(newFullPath);
        }
        else
        {
            //vpk改成其他扩展名了   删除
            OnVpkFileDeletedHandler(oldFullPath);
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

    private string? _pathCore;

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

    private string PathCore
    {
        get
        {
            if (_pathCore != null) return _pathCore;

            _pathCore = System.IO.Path.GetFullPath(Path);
            return _pathCore;
        }
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

            if (x.PathCore == y.PathCore) return 0;

            //根据创建日期从大到小排序
            int creationTimeComparison = y._creationTimeCore.CompareTo(x._creationTimeCore);
            if (creationTimeComparison != 0) return creationTimeComparison;

            //如果创建日期相同则通过名称从小到大排序
            return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
        }
    }
}
