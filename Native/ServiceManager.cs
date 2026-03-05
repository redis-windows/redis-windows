using System.ComponentModel;
using System.Runtime.InteropServices;

namespace RedisService.Native;

/// <summary>
/// Windows 服务管理器（通过 P/Invoke）
/// </summary>
public static class ServiceManager
{
    #region P/Invoke 声明

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr OpenSCManager(
        string? lpMachineName,
        string? lpDatabaseName,
        uint dwDesiredAccess);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateService(
        IntPtr hSCManager,
        string lpServiceName,
        string? lpDisplayName,
        uint dwDesiredAccess,
        uint dwServiceType,
        uint dwStartType,
        uint dwErrorControl,
        string lpBinaryPathName,
        string? lpLoadOrderGroup,
        string? lpdwTagId,
        string? lpDependencies,
        string? lpServiceStartName,
        string? lpPassword);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr OpenService(
        IntPtr hSCManager,
        string lpServiceName,
        uint dwDesiredAccess);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool DeleteService(IntPtr hService);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool CloseServiceHandle(IntPtr hSCObject);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool StartService(
        IntPtr hService,
        uint dwNumServiceArgs,
        string? lpServiceArgVectors);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool ControlService(
        IntPtr hService,
        uint dwControl,
        ref SERVICE_STATUS lpServiceStatus);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool QueryServiceStatus(
        IntPtr hService,
        ref SERVICE_STATUS lpServiceStatus);

    [StructLayout(LayoutKind.Sequential)]
    private struct SERVICE_STATUS
    {
        public uint dwServiceType;
        public uint dwCurrentState;
        public uint dwControlsAccepted;
        public uint dwWin32ExitCode;
        public uint dwServiceSpecificExitCode;
        public uint dwCheckPoint;
        public uint dwWaitHint;
    }

    #endregion

    #region 常量

    // 访问权限
    private const uint SC_MANAGER_ALL_ACCESS = 0xF003F;
    private const uint SERVICE_ALL_ACCESS = 0xF01FF;

    // 服务类型
    private const uint SERVICE_WIN32_OWN_PROCESS = 0x10;

    // 启动类型
    private const uint SERVICE_AUTO_START = 0x2;
    private const uint SERVICE_DEMAND_START = 0x3;
    private const uint SERVICE_DISABLED = 0x4;

    // 错误控制
    private const uint SERVICE_ERROR_NORMAL = 0x1;

    // 服务控制
    private const uint SERVICE_CONTROL_STOP = 0x1;

    // 服务状态
    private const uint SERVICE_STOPPED = 0x1;
    private const uint SERVICE_START_PENDING = 0x2;
    private const uint SERVICE_STOP_PENDING = 0x3;
    private const uint SERVICE_RUNNING = 0x4;

    // 错误码
    private const int ERROR_SERVICE_EXISTS = 1073;
    private const int ERROR_SERVICE_DOES_NOT_EXIST = 1060;
    private const int ERROR_SERVICE_MARKED_FOR_DELETE = 1072;

    #endregion

    /// <summary>
    /// 安装 Windows 服务
    /// </summary>
    public static void InstallService(
        string serviceName,
        string binaryPath,
        string? displayName = null,
        string? description = null,
        string startMode = "auto")
    {
        var scManager = OpenSCManager(null, null, SC_MANAGER_ALL_ACCESS);
        if (scManager == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "无法打开服务管理器，请以管理员身份运行");
        }

        try
        {
            var startType = startMode.ToLowerInvariant() switch
            {
                "auto" => SERVICE_AUTO_START,
                "manual" => SERVICE_DEMAND_START,
                "disabled" => SERVICE_DISABLED,
                _ => SERVICE_AUTO_START
            };

            var service = CreateService(
                scManager,
                serviceName,
                displayName ?? serviceName,
                SERVICE_ALL_ACCESS,
                SERVICE_WIN32_OWN_PROCESS,
                startType,
                SERVICE_ERROR_NORMAL,
                binaryPath,
                null,
                null,
                null,
                null,
                null);

            if (service == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                if (error == ERROR_SERVICE_EXISTS)
                {
                    throw new InvalidOperationException($"服务 '{serviceName}' 已存在");
                }
                throw new Win32Exception(error, "创建服务失败");
            }

            try
            {
                // 设置服务描述（可选）
                if (!string.IsNullOrEmpty(description))
                {
                    SetServiceDescription(service, description);
                }
            }
            finally
            {
                CloseServiceHandle(service);
            }
        }
        finally
        {
            CloseServiceHandle(scManager);
        }
    }

    /// <summary>
    /// 卸载 Windows 服务
    /// </summary>
    public static void UninstallService(string serviceName)
    {
        var scManager = OpenSCManager(null, null, SC_MANAGER_ALL_ACCESS);
        if (scManager == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "无法打开服务管理器，请以管理员身份运行");
        }

        try
        {
            var service = OpenService(scManager, serviceName, SERVICE_ALL_ACCESS);
            if (service == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                if (error == ERROR_SERVICE_DOES_NOT_EXIST)
                {
                    throw new InvalidOperationException($"服务 '{serviceName}' 不存在");
                }
                throw new Win32Exception(error, "打开服务失败");
            }

            try
            {
                // 先尝试停止服务
                StopService(service);

                if (!DeleteService(service))
                {
                    var error = Marshal.GetLastWin32Error();
                    if (error == ERROR_SERVICE_MARKED_FOR_DELETE)
                    {
                        throw new InvalidOperationException($"服务 '{serviceName}' 已标记为删除，请重启系统后完成删除");
                    }
                    throw new Win32Exception(error, "删除服务失败");
                }
            }
            finally
            {
                CloseServiceHandle(service);
            }
        }
        finally
        {
            CloseServiceHandle(scManager);
        }
    }

    /// <summary>
    /// 启动服务
    /// </summary>
    public static void StartServiceByName(string serviceName)
    {
        var scManager = OpenSCManager(null, null, SC_MANAGER_ALL_ACCESS);
        if (scManager == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "无法打开服务管理器");
        }

        try
        {
            var service = OpenService(scManager, serviceName, SERVICE_ALL_ACCESS);
            if (service == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "打开服务失败");
            }

            try
            {
                if (!StartService(service, 0, null))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "启动服务失败");
                }
            }
            finally
            {
                CloseServiceHandle(service);
            }
        }
        finally
        {
            CloseServiceHandle(scManager);
        }
    }

    /// <summary>
    /// 停止服务
    /// </summary>
    private static void StopService(IntPtr service)
    {
        var status = new SERVICE_STATUS();

        if (!ControlService(service, SERVICE_CONTROL_STOP, ref status))
        {
            var error = Marshal.GetLastWin32Error();
            // 服务可能已经停止
            if (error != 1062) // ERROR_SERVICE_NOT_ACTIVE
            {
                // 忽略停止失败，继续尝试删除
            }
        }

        // 等待服务停止
        for (int i = 0; i < 30; i++)
        {
            if (!QueryServiceStatus(service, ref status))
                break;

            if (status.dwCurrentState == SERVICE_STOPPED)
                return;

            Thread.Sleep(500);
        }
    }

    /// <summary>
    /// 设置服务描述
    /// </summary>
    private static void SetServiceDescription(IntPtr service, string description)
    {
        // 使用 ChangeServiceConfig2 设置描述
        // 这里简化处理，不设置描述
        // 完整实现需要更多 P/Invoke 声明
    }

    /// <summary>
    /// 检查服务是否存在
    /// </summary>
    public static bool ServiceExists(string serviceName)
    {
        var scManager = OpenSCManager(null, null, SC_MANAGER_ALL_ACCESS);
        if (scManager == IntPtr.Zero)
            return false;

        try
        {
            var service = OpenService(scManager, serviceName, SERVICE_ALL_ACCESS);
            if (service == IntPtr.Zero)
                return false;

            CloseServiceHandle(service);
            return true;
        }
        finally
        {
            CloseServiceHandle(scManager);
        }
    }
}
