namespace RedisService.CommandLine;

/// <summary>
/// 命令类型
/// </summary>
public enum CommandType
{
    /// <summary>
    /// 显示帮助
    /// </summary>
    Help,

    /// <summary>
    /// 显示版本
    /// </summary>
    Version,

    /// <summary>
    /// 安装服务
    /// </summary>
    Install,

    /// <summary>
    /// 卸载服务
    /// </summary>
    Uninstall,

    /// <summary>
    /// 运行 Redis
    /// </summary>
    Run
}

/// <summary>
/// 命令行解析结果基类
/// </summary>
public abstract class CommandResult(CommandType type)
{
    public CommandType Type { get; } = type;
}

/// <summary>
/// 帮助命令结果
/// </summary>
public class HelpCommand : CommandResult
{
    public HelpCommand() : base(CommandType.Help) { }
}

/// <summary>
/// 版本命令结果
/// </summary>
public class VersionCommand : CommandResult
{
    public VersionCommand() : base(CommandType.Version) { }
}

/// <summary>
/// 运行选项
/// </summary>
public class RunOptions
{
    /// <summary>
    /// 配置文件路径
    /// </summary>
    public string ConfigFilePath { get; set; } = "redis.conf";

    /// <summary>
    /// Redis 端口
    /// </summary>
    public int? Port { get; set; }

    /// <summary>
    /// 数据目录
    /// </summary>
    public string? DataDirectory { get; set; }

    /// <summary>
    /// 日志级别
    /// </summary>
    public string? LogLevel { get; set; }

    /// <summary>
    /// 前台运行模式
    /// </summary>
    public bool Foreground { get; set; }

    /// <summary>
    /// 作为 Windows 服务运行
    /// </summary>
    public bool AsService { get; set; }
}

/// <summary>
/// 运行命令结果
/// </summary>
public class RunCommand : CommandResult
{
    public RunOptions Options { get; }

    public RunCommand(RunOptions options) : base(CommandType.Run)
    {
        Options = options;
    }
}

/// <summary>
/// 安装选项
/// </summary>
public class InstallOptions : RunOptions
{
    /// <summary>
    /// 服务名称
    /// </summary>
    public string ServiceName { get; set; } = "Redis";

    /// <summary>
    /// 服务显示名称
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// 服务描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 启动类型: auto, manual, disabled
    /// </summary>
    public string StartMode { get; set; } = "auto";
}

/// <summary>
/// 安装命令结果
/// </summary>
public class InstallCommand : CommandResult
{
    public InstallOptions Options { get; }

    public InstallCommand(InstallOptions options) : base(CommandType.Install)
    {
        Options = options;
    }
}

/// <summary>
/// 卸载选项
/// </summary>
public class UninstallOptions
{
    /// <summary>
    /// 服务名称
    /// </summary>
    public string ServiceName { get; set; } = "Redis";
}

/// <summary>
/// 卸载命令结果
/// </summary>
public class UninstallCommand : CommandResult
{
    public UninstallOptions Options { get; }

    public UninstallCommand(UninstallOptions options) : base(CommandType.Uninstall)
    {
        Options = options;
    }
}
