namespace RedisService.CommandLine;

/// <summary>
/// 命令行解析器
/// </summary>
public static class CommandLineParser
{
    /// <summary>
    /// 解析命令行参数
    /// </summary>
    public static CommandResult Parse(string[] args)
    {
        if (args.Length == 0)
            return new HelpCommand();

        // 检查帮助和版本标志
        if (HasFlag(args, "-h", "--help"))
            return new HelpCommand();

        if (HasFlag(args, "-v", "--version"))
            return new VersionCommand();

        // 解析命令
        var command = args[0].ToLowerInvariant();

        return command switch
        {
            "install" => ParseInstallCommand(args),
            "uninstall" => ParseUninstallCommand(args),
            "run" => ParseRunCommand(args, 1),
            _ => ParseRunCommand(args, 0) // 默认为 run 命令
        };
    }

    private static InstallCommand ParseInstallCommand(string[] args)
    {
        var options = new InstallOptions();
        ParseRunOptions(args, 1, options);

        // 解析安装特有选项
        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--service-name":
                    if (i + 1 < args.Length)
                    {
                        options.ServiceName = args[++i];
                    }
                    break;

                case "--display-name":
                    if (i + 1 < args.Length)
                    {
                        options.DisplayName = args[++i];
                    }
                    break;

                case "--description":
                    if (i + 1 < args.Length)
                    {
                        options.Description = args[++i];
                    }
                    break;

                case "--start-mode":
                    if (i + 1 < args.Length)
                    {
                        options.StartMode = args[++i].ToLowerInvariant();
                    }
                    break;
            }
        }

        return new InstallCommand(options);
    }

    private static UninstallCommand ParseUninstallCommand(string[] args)
    {
        var options = new UninstallOptions();

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--service-name":
                    if (i + 1 < args.Length)
                    {
                        options.ServiceName = args[++i];
                    }
                    break;
            }
        }

        return new UninstallCommand(options);
    }

    private static RunCommand ParseRunCommand(string[] args, int startIndex)
    {
        var options = new RunOptions();
        ParseRunOptions(args, startIndex, options);
        options.AsService = !options.Foreground;
        return new RunCommand(options);
    }

    private static void ParseRunOptions(string[] args, int startIndex, RunOptions options)
    {
        for (int i = startIndex; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "-c":
                case "--config":
                    if (i + 1 < args.Length)
                    {
                        options.ConfigFilePath = args[++i];
                    }
                    break;

                case "--port":
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out var port))
                    {
                        options.Port = port;
                        i++;
                    }
                    break;

                case "--dir":
                    if (i + 1 < args.Length)
                    {
                        options.DataDirectory = args[++i];
                    }
                    break;

                case "--loglevel":
                    if (i + 1 < args.Length)
                    {
                        options.LogLevel = args[++i];
                    }
                    break;

                case "-f":
                case "--foreground":
                    options.Foreground = true;
                    break;
            }
        }
    }

    private static bool HasFlag(string[] args, params string[] flags)
    {
        return args.Any(arg => flags.Contains(arg, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 显示帮助信息
    /// </summary>
    public static void PrintHelp()
    {
        Console.WriteLine(@"
RedisService - Redis Windows Service Wrapper

用法: RedisService [command] [options]

命令:
  install       安装为 Windows 服务
  uninstall     卸载 Windows 服务
  run           运行 Redis（默认命令）

选项:
  -c, --config <FILE>      Redis 配置文件路径 (默认: redis.conf)
  --port <PORT>            覆盖 Redis 端口
  --dir <DIRECTORY>        覆盖 Redis 数据目录
  --loglevel <LEVEL>       日志级别 (debug, verbose, notice, warning)
  -f, --foreground         前台运行模式
  --service-name <NAME>    服务名称 (默认: Redis)
  --display-name <NAME>    服务显示名称
  --description <TEXT>     服务描述
  --start-mode <MODE>      启动类型: auto, manual (默认: auto)
  -h, --help               显示帮助
  -v, --version            显示版本

示例:
  RedisService.exe install -c redis.conf --port 6380
  RedisService.exe run --foreground
  RedisService.exe uninstall
  RedisService.exe uninstall --service-name MyRedis
");
    }

    /// <summary>
    /// 显示版本信息
    /// </summary>
    public static void PrintVersion()
    {
        var version = typeof(CommandLineParser).Assembly.GetName().Version;
        Console.WriteLine($"RedisService version {version?.ToString() ?? "1.0.0"}");
        Console.WriteLine("Redis Windows Service Wrapper");
        Console.WriteLine("https://github.com/redis-windows/redis-windows");
    }
}
