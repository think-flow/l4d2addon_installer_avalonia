// ReSharper disable InconsistentNaming

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace l4d2addon_installer;

/// <summary>
/// 微软在win11上推出的效能模式
/// https://devblogs.microsoft.com/performance-diagnostics/introducing-ecoqos/
/// </summary>
[SupportedOSPlatform("windows")]
public static partial class EcoQosProcess
{
    private static readonly IntPtr pThrottleOn;
    private static readonly IntPtr pThrottleOff;
    private static readonly int szControlBlock;

    static EcoQosProcess()
    {
        szControlBlock = Marshal.SizeOf<PROCESS_POWER_THROTTLING_STATE>();
        pThrottleOn = Marshal.AllocHGlobal(szControlBlock);
        pThrottleOff = Marshal.AllocHGlobal(szControlBlock);

        var throttleState = new PROCESS_POWER_THROTTLING_STATE
        {
            Version = PROCESS_POWER_THROTTLING_STATE.PROCESS_POWER_THROTTLING_CURRENT_VERSION,
            ControlMask = ProcessorPowerThrottlingFlags.PROCESS_POWER_THROTTLING_EXECUTION_SPEED,
            StateMask = ProcessorPowerThrottlingFlags.PROCESS_POWER_THROTTLING_EXECUTION_SPEED
        };

        var unthrottleState = new PROCESS_POWER_THROTTLING_STATE
        {
            Version = PROCESS_POWER_THROTTLING_STATE.PROCESS_POWER_THROTTLING_CURRENT_VERSION,
            ControlMask = ProcessorPowerThrottlingFlags.PROCESS_POWER_THROTTLING_EXECUTION_SPEED,
            StateMask = ProcessorPowerThrottlingFlags.None
        };

        Marshal.StructureToPtr(throttleState, pThrottleOn, false);
        Marshal.StructureToPtr(unthrottleState, pThrottleOff, false);
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetProcessInformation(IntPtr hProcess, PROCESS_INFORMATION_CLASS ProcessInformationClass, IntPtr ProcessInformation, uint ProcessInformationSize);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetPriorityClass(IntPtr handle, PriorityClass priorityClass);

    public static void EnableEcoQos()
    {
        ToggleEfficiencyMode(Process.GetCurrentProcess().Handle, true);
    }

    public static void DisableEcoQos()
    {
        ToggleEfficiencyMode(Process.GetCurrentProcess().Handle, false);
    }

    private static void ToggleEfficiencyMode(IntPtr hProcess, bool enable)
    {
        _ = SetProcessInformation(
            hProcess,
            PROCESS_INFORMATION_CLASS.ProcessPowerThrottling,
            enable ? pThrottleOn : pThrottleOff,
            (uint) szControlBlock
        );
        _ = SetPriorityClass(hProcess, enable ? PriorityClass.IDLE_PRIORITY_CLASS : PriorityClass.NORMAL_PRIORITY_CLASS);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_POWER_THROTTLING_STATE
    {
        public const uint PROCESS_POWER_THROTTLING_CURRENT_VERSION = 1;
        public uint Version;
        public ProcessorPowerThrottlingFlags ControlMask;
        public ProcessorPowerThrottlingFlags StateMask;
    }

    [Flags]
    private enum ProcessorPowerThrottlingFlags : uint
    {
        None = 0x0,
        PROCESS_POWER_THROTTLING_EXECUTION_SPEED = 0x1
    }

    private enum PROCESS_INFORMATION_CLASS
    {
        ProcessMemoryPriority,
        ProcessMemoryExhaustionInfo,
        ProcessAppMemoryInfo,
        ProcessInPrivateInfo,
        ProcessPowerThrottling,
        ProcessReservedValue1,
        ProcessTelemetryCoverageInfo,
        ProcessProtectionLevelInfo,
        ProcessLeapSecondInfo,
        ProcessInformationClassMax
    }

    private enum PriorityClass : uint
    {
        ABOVE_NORMAL_PRIORITY_CLASS = 0x8000,
        BELOW_NORMAL_PRIORITY_CLASS = 0x4000,
        HIGH_PRIORITY_CLASS = 0x80,
        IDLE_PRIORITY_CLASS = 0x40,
        NORMAL_PRIORITY_CLASS = 0x20,
        PROCESS_MODE_BACKGROUND_BEGIN = 0x100000, // 'Windows Vista/2008 and higher
        PROCESS_MODE_BACKGROUND_END = 0x200000, //   'Windows Vista/2008 and higher
        REALTIME_PRIORITY_CLASS = 0x100
    }
}
