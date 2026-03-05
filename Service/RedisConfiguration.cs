using System.Text;

namespace RedisService.Service;

/// <summary>
/// Redis 服务配置
/// </summary>
public class RedisConfiguration
{
    /// <summary>
    /// Redis 配置文件路径
    /// </summary>
    public string ConfigFilePath { get; init; } = "redis.conf";

    /// <summary>
    /// Redis 端口（覆盖配置文件中的设置）
    /// </summary>
    public int? Port { get; init; }

    /// <summary>
    /// Redis 数据目录（覆盖配置文件中的设置）
    /// </summary>
    public string? DataDirectory { get; init; }

    /// <summary>
    /// 日志级别（覆盖配置文件中的设置）
    /// </summary>
    public string? LogLevel { get; init; }

    /// <summary>
    /// 优雅关闭超时时间（毫秒）
    /// </summary>
    public int GracefulShutdownTimeoutMs { get; init; } = 5000;

    /// <summary>
    /// 进程启动超时时间（毫秒）
    /// </summary>
    public int ProcessStartTimeoutMs { get; init; } = 3000;

    /// <summary>
    /// 将 Windows 路径转换为 Cygwin/MSYS2 风格路径
    /// </summary>
    /// <param name="windowsPath">Windows 路径</param>
    /// <returns>Cygwin 风格路径 (如 /cygdrive/c/path)</returns>
    public static string ToCygwinPath(string windowsPath)
    {
        var path = Path.GetFullPath(windowsPath);
        var colonIndex = path.IndexOf(':');
        if (colonIndex > 0)
        {
            var drive = path[..colonIndex].ToLower();
            return path
                .Remove(0, colonIndex + 1)
                .Insert(0, $"/cygdrive/{drive}")
                .Replace('\\', '/');
        }
        return path.Replace('\\', '/');
    }

    /// <summary>
    /// 获取 Cygwin 风格的配置文件路径
    /// </summary>
    public string GetCygwinConfigPath()
    {
        return ToCygwinPath(ConfigFilePath);
    }

    /// <summary>
    /// 获取 Cygwin 风格的数据目录路径
    /// </summary>
    public string? GetCygwinDataDirectory()
    {
        return DataDirectory != null ? ToCygwinPath(DataDirectory) : null;
    }

    /// <summary>
    /// 构建 redis-server 命令行参数
    /// </summary>
    public string BuildArguments()
    {
        var args = new StringBuilder();

        // 配置文件（必需，用于 Cygwin 路径）
        args.Append($"\"{GetCygwinConfigPath()}\"");

        // 覆盖选项
        if (Port.HasValue)
            args.Append($" --port {Port.Value}");

        if (!string.IsNullOrEmpty(DataDirectory))
            args.Append($" --dir \"{GetCygwinDataDirectory()}\"");

        if (!string.IsNullOrEmpty(LogLevel))
            args.Append($" --loglevel {LogLevel}");

        return args.ToString();
    }

    /// <summary>
    /// 构建 redis-cli SHUTDOWN 命令行参数
    /// </summary>
    public string BuildCliShutdownArguments()
    {
        var args = new StringBuilder();

        // 传递配置文件路径（确保 redis-cli 知道正确的 dir）
        args.Append($"\"{GetCygwinConfigPath()}\"");

        // 传递端口覆盖
        if (Port.HasValue)
            args.Append($" -p {Port.Value}");

        // 传递 dir 覆盖
        if (!string.IsNullOrEmpty(DataDirectory))
            args.Append($" --dir \"{GetCygwinDataDirectory()}\"");

        // SHUTDOWN 命令
        args.Append(" SHUTDOWN");

        return args.ToString();
    }
}
