using System;

namespace l4d2addon_installer.Models;

/// <summary>
/// 应用程序配置文件
/// </summary>
[Serializable]
public class AppConfig
{
    /// <summary>
    /// 左边框宽度占比
    /// </summary>
    public double? LeftColWidthPercent { get; set; }

    /// <summary>
    /// 是否 安装vpk时，是否覆盖文件
    /// </summary>
    public bool? IsCoverd { get; set; }
}
