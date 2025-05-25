using System;

namespace l4d2addon_installer.Models;

public class VpkFileInfo
{
    /// <summary>
    /// 文件名称（带扩展名）
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// 文件名称（不带扩展名）
    /// </summary>
    public required string NameWithoutEx { get; set; }

    /// <summary>
    /// 文件创建日期
    /// </summary>
    public DateTimeOffset CreationTime { get; set; }

    /// <summary>
    /// 文件修改日期
    /// </summary>
    public DateTimeOffset ModifiedTime { get; set; }

    /// <summary>
    /// 文件全路径
    /// </summary>
    public required string FullPath { get; set; }

    /// <summary>
    /// 文件大小
    /// </summary>
    public long Size { get; set; }
}
