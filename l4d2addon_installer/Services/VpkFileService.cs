using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Threading.Tasks;
using l4d2addon_installer.Models;
using l4d2addon_installer.ViewModels;
using Microsoft.Win32;
using Serilog;
using SharpCompress.Archives;
using SharpCompress.Common;

// ReSharper disable InconsistentNaming

namespace l4d2addon_installer.Services;

public partial class VpkFileService
{
    private readonly Channel<FileWatcherMessage> _channel = Channel.CreateUnbounded<FileWatcherMessage>();
    private string? _addonPath;
    private FileSystemWatcher? _fileWatcher;
    private string? _gamePath;
    private string? _steamPath;

    public VpkFileService()
    {
        WatcherAddons(GetAddonsPath());
    }

    public ChannelReader<FileWatcherMessage> FileWatcherMessageReader => _channel.Reader;

    /// <summary>
    /// 在文件管理器中显示文件
    /// </summary>
    /// <param name="fileInfo"></param>
    /// <exception cref="ServiceException"></exception>
    public Task RevealFileAsync(VpkFileInfoViewModel fileInfo)
    {
        if (OperatingSystem.IsWindows())
        {
            return RevealFileOnWindowsAsync(fileInfo);
        }

        if (OperatingSystem.IsLinux())
        {
            return RevealFileOnLinuxAsync(fileInfo);
        }

        throw new UnreachableException("Not supported OperatingSystem");
    }

    [SupportedOSPlatform("windows")]
    private async Task RevealFileOnWindowsAsync(VpkFileInfoViewModel fileInfo)
    {
        int exitCode;
        try
        {
            exitCode = await Task.Run(() =>
            {
                var process = Process.Start("explorer.exe", $"/select, \"{fileInfo.Path}\"");
                process.WaitForExit();
                //进程退出代码如果不是1的话，代表explorer执行出错，我们返回false通知调用人员，代码是否执行成功
                return process.ExitCode;
            });
        }
        catch (Exception e)
        {
            throw new ServiceException("explorer执行出错: " + e.Message, e);
        }

        if (exitCode != 1) throw new ServiceException($"VpkFileService.RevealFileAsync returned exit code [{exitCode}]");
    }

    [SupportedOSPlatform("linux")]
    private Task RevealFileOnLinuxAsync(VpkFileInfoViewModel fileInfo)
    {
        // 尝试使用支持 --select 的文件管理器
        string[] fileManagers = new[] {"nautilus", "dolphin"};
        foreach (string fm in fileManagers)
        {
            if (IsCommandAvailable(fm))
            {
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = fm,
                        Arguments = $"--select \"{fileInfo.Path}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var p = Process.Start(startInfo);
                    if (p != null)
                    {
                        return Task.CompletedTask; // 成功启动
                    }
                }
                catch
                {
                    // 忽略，尝试下一个
                }
            }
        }

        // 回退：用 xdg-open 打开所在目录（不选中文件）
        return OpenPathAsync(Path.GetDirectoryName(fileInfo.Path)!);

        // 检查命令是否存在
        static bool IsCommandAvailable(string command)
        {
            try
            {
                using var p = Process.Start(new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                });
                p?.WaitForExit();
                return p?.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// 获取 Left 4 Dead 2 的vpk文件信息
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ServiceException"></exception>
    public async Task<List<VpkFileInfo>> GetVpkFilesAsync()
    {
        string addonsPath = GetAddonsPath();
        string[] vpkFilePaths = Directory.GetFiles(addonsPath, "*.vpk", SearchOption.TopDirectoryOnly);

        var list = new List<VpkFileInfo>(vpkFilePaths.Length);
        foreach (string vpkFilePath in vpkFilePaths)
        {
            var vpkFileInfo = await GetVpkFileAsync(vpkFilePath);
            list.Add(vpkFileInfo);
        }

        return list;
    }

    /// <summary>
    /// 获取 Left 4 Dead 2 的vpk文件信息
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ServiceException"></exception>
    public Task<VpkFileInfo> GetVpkFileAsync(string vpkFilePath)
    {
        return Task.Run(() =>
        {
            FileInfo fileInfo;
            try
            {
                fileInfo = new FileInfo(vpkFilePath);
            }
            catch (Exception e)
            {
                throw new ServiceException($"未能读取{{vpkFilePath}}文件信息：{e.Message}", e);
            }

            var vpkFileInfo = new VpkFileInfo
            {
                Name = fileInfo.Name,
                NameWithoutEx = fileInfo.Name.Substring(0, fileInfo.Name.Length - fileInfo.Extension.Length),
                CreationTime = fileInfo.CreationTime,
                ModifiedTime = fileInfo.LastWriteTime,
                FullPath = fileInfo.FullName,
                Size = fileInfo.Length
            };
            return vpkFileInfo;
        });
    }

    /// <summary>
    /// 删除vpk文件
    /// </summary>
    public async Task DeleteVpkFilesAsync(IList<VpkFileInfoViewModel> fileInfos, bool toTrash,
        Action<string?, AggregateException?> progressCallback)
    {
        var tasks = new List<Task>(fileInfos.Count);
        foreach (var fileInfo in fileInfos)
        {
            var task = DeleteCore(fileInfo, toTrash, progressCallback);
            _ = task.ContinueWith(t => progressCallback(null, t.Exception), TaskContinuationOptions.OnlyOnFaulted);
            tasks.Add(task);
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception)
        {
            // ignored
            //捕获所有异常，并忽略
            //因为所有异常都转交给 progressCallback了
        }

        return;

        static Task DeleteCore(VpkFileInfoViewModel fileInfo, bool toTrash, Action<string?, AggregateException?> progressCallback)
        {
            return Task.Run(() =>
            {
                if (toTrash)
                {
                    //将文件移到回收站
                    try
                    {
                        RecycleBinHelper.DeleteToRecycleBin(fileInfo.Path);
                        progressCallback($"{fileInfo.Name} 已成功移至回收站", null);
                    }
                    catch (Exception e)
                    {
                        throw new ServiceException($"文件移至回收站时出错：{e.Message}", e);
                    }
                }
                else
                {
                    //直接删除文件
                    try
                    {
                        File.Delete(fileInfo.Path);
                        progressCallback($"{fileInfo.Name} 删除成功", null);
                    }
                    catch (Exception e)
                    {
                        throw new ServiceException($"文件删除时出错：{e.Message}", e);
                    }
                }
            });
        }
    }

    /// <summary>
    /// 安装vpk文件 支持.vpk .zip .rar
    /// </summary>
    public async Task InstallVpkFilesAsync(IList<string> filePaths, bool isCoverd,
        Action<string?, AggregateException?> progressCallback)
    {
        ArgumentNullException.ThrowIfNull(progressCallback);

        //错误处理流程
        //如果是不影响接下逻辑执行的，则使用progressCallback 抛出
        //如果错误是会中断流程的，则使用throw抛出

        string addonsPath = GetAddonsPath();
        var tasks = new List<Task>(filePaths.Count);
        foreach (string filePath in filePaths)
        {
            string ext = Path.GetExtension(filePath);

            if (".vpk".Equals(ext, StringComparison.OrdinalIgnoreCase))
            {
                var task = VpkInstaller(filePath, addonsPath, isCoverd, progressCallback);
                RegisterFaultedContinueTask(task, progressCallback);
                tasks.Add(task);
            }
            else if (".zip".Equals(ext, StringComparison.OrdinalIgnoreCase)
                     || ".rar".Equals(ext, StringComparison.OrdinalIgnoreCase))
            {
                //在SharpCompress 解压.zip 和.rar 使用同一套代码
                var task = ZipInstaller(filePath, addonsPath, isCoverd, progressCallback);
                RegisterFaultedContinueTask(task, progressCallback);
                tasks.Add(task);
            }
            else
            {
                progressCallback(null, new AggregateException(new ServiceException($"{Path.GetFileName(filePath)} --> 不支持该文件格式")));
            }
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception)
        {
            // ignored
            //捕获所有异常，并忽略
            //因为所有异常都转交给 progressCallback了
        }


        static Task VpkInstaller(string filePath, string addonsPath, bool isCoverd,
            Action<string?, AggregateException?> progressCallback)
        {
            return Task.Run(() =>
            {
                string fileName = Path.GetFileName(filePath);
                string destPath = Path.Combine(addonsPath, fileName);
                try
                {
                    File.Copy(filePath, destPath, isCoverd);
                    progressCallback($"{Path.GetFileName(filePath)} 已安装", null);
                }
                catch (IOException)
                {
                    //当isCoverd为true时，目标文件已存在，则会抛出IOException
                    throw new ServiceException($"{fileName} 已存在");
                }
            });
        }

        static Task ZipInstaller(string filePath, string addonsPath, bool isCoverd,
            Action<string?, AggregateException?> progressCallback)
        {
            return Task.Factory.StartNew(() =>
            {
                using var archive = ArchiveFactory.Open(filePath);
                string fileName = Path.GetFileName(filePath);
                var entries = archive.Entries.Where(entry =>
                    !entry.IsDirectory
                    && ".vpk".Equals(Path.GetExtension(entry.Key), StringComparison.OrdinalIgnoreCase)).ToArray();

                if (entries.Length == 0)
                {
                    throw new ServiceException($"{fileName} --> 未找到需要安装的vpk文件");
                }

                if (entries.Any(e => e.IsEncrypted))
                {
                    throw new ServiceException($"{fileName} --> 不支持的加密压缩包");
                }

                progressCallback($"{fileName} --> {entries.Select(entry => $"\"{entry.Key}\"").Aggregate((pv, cv) => $"{pv}, {cv}")}", null);

                //写入文件
                foreach (var entry in entries)
                {
                    if (entry.Key is null) continue;

                    if (!isCoverd)
                    {
                        if (File.Exists(Path.Combine(addonsPath, entry.Key)))
                        {
                            progressCallback(null, new AggregateException(new ServiceException($"{fileName} --> {entry.Key} 已存在")));
                            continue;
                        }
                    }

                    entry.WriteToDirectory(addonsPath, new ExtractionOptions
                    {
                        Overwrite = isCoverd,
                        ExtractFullPath = false
                    });
                    progressCallback($"{fileName} --> {entry.Key} 已安装", null);
                }
            }, TaskCreationOptions.LongRunning);
        }

        static void RegisterFaultedContinueTask(Task task, Action<string?, AggregateException?>? progressCallback)
        {
            if (progressCallback is not null)
            {
                task.ContinueWith(t => progressCallback(null, t.Exception), TaskContinuationOptions.OnlyOnFaulted);
            }
        }
    }

    /// <summary>
    /// 获取Steam路径
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ServiceException"></exception>
    public string GetSteamPath()
    {
        if (OperatingSystem.IsWindows())
        {
            return GetSteamPathOnWindows();
        }

        if (OperatingSystem.IsLinux())
        {
            return GetSteamPathOnLinux();
        }

        // 不受支持的操作系统
        throw new UnreachableException("Not supported OperatingSystem");
    }

    [SupportedOSPlatform("windows")]
    private string GetSteamPathOnWindows()
    {
        if (_steamPath != null) return _steamPath;

        try
        {
            // 打开HKEY_CURRENT_USER下的Steam注册表项
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            if (key != null)
            {
                // 读取SteamPath的值
                string? value = key.GetValue("SteamPath")?.ToString();
                if (value != null)
                {
                    _steamPath = new Uri(value).LocalPath;
                    return _steamPath;
                }
            }

            throw new ServiceException("未找到Steam安装路径");
        }
        catch (Exception e)
        {
            throw new ServiceException("读取注册表出错: " + e.Message, e);
        }
    }

    [SupportedOSPlatform("linux")]
    private string GetSteamPathOnLinux()
    {
        if (_steamPath != null) return _steamPath;
        // 常见的 Linux Steam 安装路径
        string[] possiblePaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "Steam"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".steam", "steam"),
            "/usr/share/steam",
            "/usr/local/share/steam"
        };

        foreach (string path in possiblePaths)
        {
            if (Directory.Exists(path))
            {
                _steamPath = path;
                return _steamPath;
            }
        }

        throw new ServiceException("未找到Steam安装路径");
    }

    /// <summary>
    /// 获取 Left 4 Dead 2 的安装路径
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ServiceException"></exception>
    public string GetGamePath()
    {
        if (_gamePath != null) return _gamePath;

        string steamPath = GetSteamPath();
        string libraryFoldersVDFFilePath = Path.Join(steamPath, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(libraryFoldersVDFFilePath))
        {
            throw new ServiceException($"未找到 {libraryFoldersVDFFilePath} 文件");
        }

        //解析 libraryfolders.vdf 文件
        string vdfContent = File.ReadAllText(libraryFoldersVDFFilePath);
        var matches = Regex.Matches(vdfContent, @"""path""\s+""([^""]+)""");
        var libraryFolders = matches.Select(match => match.Groups[1].Value);

        foreach (string folder in libraryFolders)
        {
            string folderPath = folder.Replace(@"\\", @"\"); // Normalize vdf中的libraryFolder路径
            string gamePath = Path.Join(folderPath, "steamapps", "common", "Left 4 Dead 2");
            if (Directory.Exists(gamePath))
            {
                _gamePath = gamePath;
                return _gamePath;
            }
        }

        //未能在vdf指定的steamapps下找到Left 4 Dead 2文件夹
        //则回退到默认的steamapps目录下寻找
        string defaultGamePath = Path.Join(steamPath, "steamapps", "common", "Left 4 Dead 2");
        if (Directory.Exists(defaultGamePath))
        {
            _gamePath = defaultGamePath;
            return _gamePath;
        }

        //如果还是没找到则抛出异常
        throw new ServiceException("未找到 Left 4 Dead 2 的安装路径");
    }

    /// <summary>
    /// 获取 Left 4 Dead 2 的 addons 文件夹路径
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ServiceException"></exception>
    public string GetAddonsPath()
    {
        if (_addonPath != null) return _addonPath;

        string gamePath = GetGamePath();
        string addonsPath = Path.Join(gamePath, "left4dead2", "addons");
        if (!Directory.Exists(addonsPath))
        {
            throw new ServiceException("未找到 Addons 文件夹路径");
        }

        _addonPath = addonsPath;
        return _addonPath;
    }

    /// <summary>
    /// 通过explorer 打开addons文件夹
    /// </summary>
    /// <exception cref="ServiceException"></exception>
    /// <returns></returns>
    public Task OpenAddonsFloderAsync()
    {
        string path = GetAddonsPath();
        return OpenPathAsync(path);
    }

    /// <summary>
    /// 通过explorer 打开l4d2文件夹
    /// </summary>
    /// <exception cref="ServiceException"></exception>
    /// <returns></returns>
    public Task OpenGameFolderAsync()
    {
        string path = GetGamePath();
        return OpenPathAsync(path);
    }

    /// <summary>
    /// 通过explorer 打开下载文件夹
    /// </summary>
    /// <exception cref="ServiceException"></exception>
    /// <returns></returns>
    public Task OpenDownloadsFolderAsync()
    {
        string path;
        if (OperatingSystem.IsWindows())
        {
            path = "shell:Downloads";
        }
        else if (OperatingSystem.IsLinux())
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "xdg-user-dir",
                Arguments = "DOWNLOAD",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new InvalidOperationException("无法启动 xdg-user-dir");
            }

            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                string error = process.StandardError.ReadToEnd();
                throw new InvalidOperationException($"xdg-user-dir failed: {error}");
            }

            string? download_path = process.StandardOutput.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(download_path))
            {
                throw new InvalidOperationException("xdg-user-dir returned empty path");
            }

            path = download_path;
        }
        else
        {
            throw new UnreachableException("Not supported OperatingSystem");
        }

        return OpenPathAsync(path);
    }

    /// <summary>
    /// 通过explorer 打开回收站
    /// </summary>
    /// <exception cref="ServiceException"></exception>
    /// <returns></returns>
    public Task OpenRecycleBinFolderAsync()
    {
        string path;
        if (OperatingSystem.IsWindows())
        {
            path = "hell:RecycleBinFolder";
        }
        else if (OperatingSystem.IsLinux())
        {
            path = "trash:///";
        }
        else
        {
            throw new UnreachableException("Not supported OperatingSystem");
        }

        return OpenPathAsync(path);
    }

    /// <summary>
    /// 启动left 4 dead 2
    /// </summary>
    /// <exception cref="ServiceException"></exception>
    /// <returns></returns>
    public async Task StartGameAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                //查询游戏是否已存在
                var result = Process.GetProcessesByName("left4dead2");
                if (result.Length > 0) throw new ServiceException("left4dead2.exe 已存在，请勿重复启动！");

                Process.Start(new ProcessStartInfo("steam://rungameid/550")
                {
                    UseShellExecute = true
                });
            });
        }
        catch (ServiceException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new ServiceException("启动游戏出错: " + e.Message, e);
        }
    }

    //通过explorer 打开路径
    private async Task OpenPathAsync(string path)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                await Task.Run(() =>
                {
                    var process = Process.Start("explorer.exe", $"\"{path}\"");
                    process.WaitForExit();
                    int exitCode = process.ExitCode;
                    if (exitCode != 1)
                    {
                        throw new ServiceException($"explorer returned exit code [{exitCode}]");
                    }
                });
                await Task.Delay(500); //延迟500毫秒 优化用户体验
            }
            else if (OperatingSystem.IsLinux())
            {
                // 使用 xdg-open 打开默认文件管理器
                var startInfo = new ProcessStartInfo("xdg-open", $"\"{path}\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    throw new ServiceException("无法启动xdg-open");
                }
            }
            else
            {
                throw new UnreachableException("Not supported OperatingSystem");
            }
        }
        catch (Exception e)
        {
            throw new ServiceException("explorer执行出错: " + e.Message, e);
        }
    }

    //监听文件夹中，文件的新增和删除
    private void WatcherAddons(string path)
    {
        _fileWatcher = new FileSystemWatcher(path, "*.vpk");
        _fileWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName;
        _fileWatcher.IncludeSubdirectories = false;
        _fileWatcher.EnableRaisingEvents = true;

        _fileWatcher.Created += (_, e) =>
        {
            Log.Debug("[FileWatcher Event Created] {filename}", e.Name);
            _channel.Writer.TryWrite(new FileWatcherMessage(FileWatcherMessage.MessageType.Created, e.FullPath, e.FullPath));
        };

        _fileWatcher.Deleted += (_, e) =>
        {
            Log.Debug("[FileWatcher Event Deleted] {filename}", e.Name);
            _channel.Writer.TryWrite(new FileWatcherMessage(FileWatcherMessage.MessageType.Deleted, e.FullPath, e.FullPath));
        };

        _fileWatcher.Renamed += (_, e) =>
        {
            Log.Debug("[FileWatcher Event Renamed] {oldfilename} -> {newfilename}", e.OldName, e.Name);
            _channel.Writer.TryWrite(new FileWatcherMessage(FileWatcherMessage.MessageType.Renamed, e.FullPath, e.OldFullPath));
        };

        _fileWatcher.Error += (_, e) =>
        {
            var ex = e.GetException();
            _fileWatcher.Dispose();
            _fileWatcher = null;
            throw new ServiceException("[Addons Watcher发生错误] " + ex.Message);
        };
    }

    //删除文件到回收站，帮助类
    private static partial class RecycleBinHelper
    {
        /// <summary>
        /// 将文件移动到回收站
        /// </summary>
        /// <param name="filePath"></param>
        /// <exception cref="IOException"></exception>
        public static void DeleteToRecycleBin(string filePath)
        {
            if (OperatingSystem.IsWindows())
            {
                DeleteToRecycleBinOnWindows(filePath);
            }
            else if (OperatingSystem.IsLinux())
            {
                DeleteToRecycleBinOnLinux(filePath);
            }
            else
            {
                throw new UnreachableException("Not supported OperatingSystem");
            }
        }

        [SupportedOSPlatform("windows")]
        private static void DeleteToRecycleBinOnWindows(string filePath)
        {
            var fileop = new SHFILEOPSTRUCT
            {
                wFunc = FO_DELETE,
                pFrom = filePath + '\0' + '\0', // 必须双终止符
                fFlags = FOF_ALLOWUNDO | FOF_NO_UI,
                fAnyOperationsAborted = false,
                hwnd = IntPtr.Zero,
                pTo = null!,
                hNameMappings = IntPtr.Zero,
                lpszProgressTitle = null!
            };

            int result = SHFileOperationW(ref fileop);
            if (result != 0)
            {
                throw new IOException($"Failed to move file to recycle bin. Error code: {result}");
            }
        }

        [SupportedOSPlatform("linux")]
        private static void DeleteToRecycleBinOnLinux(string filePath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "gio",
                Arguments = $"trash \"{filePath}\"",
                UseShellExecute = false,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            process?.WaitForExit();

            if (process?.ExitCode != 0)
            {
                string? error = process?.StandardError.ReadToEnd();
                throw new InvalidOperationException($"无法移入回收站: {error}");
            }
        }


        #region RecycleBinHelper 核心

        [SupportedOSPlatform("windows")]
        private const int FO_DELETE = 3;

        [SupportedOSPlatform("windows")]
        private const int FOF_ALLOWUNDO = 0x40;

        [SupportedOSPlatform("windows")]
        private const int FOF_NO_UI = 0x0614; // FOF_SILENT | FOF_NOCONFIRMATION | FOF_NOERRORUI

        [SupportedOSPlatform("windows")]
        [LibraryImport("shell32.dll", SetLastError = true)]
        private static partial int SHFileOperationW(ref SHFILEOPSTRUCT lpFileOp);

        [SupportedOSPlatform("windows")]
        [NativeMarshalling(typeof(SHFILEOPSTRUCTMarshaller))]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEOPSTRUCT
        {
            public nint hwnd;
            public int wFunc;
            public string? pFrom;
            public string? pTo;
            public ushort fFlags;
            public bool fAnyOperationsAborted;
            public nint hNameMappings;
            public string? lpszProgressTitle;
        }

        //SHFILEOPSTRUCT 结构体的封送程序
        [SupportedOSPlatform("windows")]
        [CustomMarshaller(typeof(SHFILEOPSTRUCT), MarshalMode.ManagedToUnmanagedRef, typeof(SHFILEOPSTRUCTMarshaller))]
        private static class SHFILEOPSTRUCTMarshaller
        {
            // 将托管结构转换为非托管内存
            public static SHFILEOPSTRUCT_Unmanaged ConvertToUnmanaged(SHFILEOPSTRUCT managed) =>
                new()
                {
                    hwnd = managed.hwnd,
                    wFunc = managed.wFunc,
                    pFrom = Marshal.StringToHGlobalUni(managed.pFrom), // 转换为双 null 终止的 Unicode 字符串
                    pTo = Marshal.StringToHGlobalUni(managed.pTo),
                    fFlags = managed.fFlags,
                    fAnyOperationsAborted = managed.fAnyOperationsAborted,
                    hNameMappings = managed.hNameMappings,
                    lpszProgressTitle = Marshal.StringToHGlobalUni(managed.lpszProgressTitle)
                };

            // 释放非托管内存
            public static void Free(SHFILEOPSTRUCT_Unmanaged unmanaged)
            {
                Marshal.FreeHGlobal(unmanaged.pFrom);
                Marshal.FreeHGlobal(unmanaged.pTo);
                Marshal.FreeHGlobal(unmanaged.lpszProgressTitle);
            }

            // 非托管到托管转换
            public static SHFILEOPSTRUCT ConvertToManaged(SHFILEOPSTRUCT_Unmanaged unmanaged) =>
                new()
                {
                    hwnd = unmanaged.hwnd,
                    wFunc = unmanaged.wFunc,
                    pFrom = Marshal.PtrToStringUni(unmanaged.pFrom) ?? null,
                    pTo = Marshal.PtrToStringUni(unmanaged.pTo) ?? null,
                    fFlags = unmanaged.fFlags,
                    fAnyOperationsAborted = unmanaged.fAnyOperationsAborted,
                    hNameMappings = unmanaged.hNameMappings,
                    lpszProgressTitle = Marshal.PtrToStringUni(unmanaged.lpszProgressTitle) ?? null
                };

            // 非托管结构体定义（严格匹配 Win32 的 SHFILEOPSTRUCTW）
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public struct SHFILEOPSTRUCT_Unmanaged
            {
                public nint hwnd;
                public int wFunc;
                public nint pFrom;
                public nint pTo;
                public ushort fFlags;
                public bool fAnyOperationsAborted;
                public nint hNameMappings;
                public nint lpszProgressTitle;
            }
        }

        #endregion
    }
}

public class FileWatcherMessage
{
    public enum MessageType
    {
        Created,
        Deleted,
        Renamed
    }

    public FileWatcherMessage(MessageType type, string filePath, string oldFilePath)
    {
        Type = type;
        FilePath = filePath;
        OldFilePath = oldFilePath;
    }

    public MessageType Type { get; }
    public string FilePath { get; }
    public string OldFilePath { get; }
}
