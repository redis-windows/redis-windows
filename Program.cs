using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RedisService.CommandLine;
using RedisService.Native;
using RedisService.Service;

namespace RedisService;

class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            var result = CommandLineParser.Parse(args);

            return result switch
            {
                HelpCommand => PrintHelp(),
                VersionCommand => PrintVersion(),
                InstallCommand cmd => InstallService(cmd),
                UninstallCommand cmd => UninstallService(cmd),
                RunCommand cmd => await RunRedis(cmd),
                _ => PrintHelp()
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"错误: {ex.Message}");
            return 1;
        }
    }

    #region 命令处理

    private static int PrintHelp()
    {
        CommandLineParser.PrintHelp();
        return 0;
    }

    private static int PrintVersion()
    {
        CommandLineParser.PrintVersion();
        return 0;
    }

    private static int InstallService(InstallCommand cmd)
    {
        var options = cmd.Options;

        Console.WriteLine($"正在安装服务 '{options.ServiceName}'...");

        // 获取当前可执行文件路径
        var exePath = Environment.ProcessPath
            ?? AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar) + ".exe";

        // 构建服务参数
        var serviceArgs = new List<string>();

        // 添加 run 命令（服务模式）
        serviceArgs.Add("run");

        // 添加配置文件参数
        serviceArgs.Add($"-c \"{Path.GetFullPath(options.ConfigFilePath)}\"");

        // 添加其他参数
        if (options.Port.HasValue)
            serviceArgs.Add($"--port {options.Port.Value}");

        if (!string.IsNullOrEmpty(options.DataDirectory))
            serviceArgs.Add($"--dir \"{options.DataDirectory}\"");

        if (!string.IsNullOrEmpty(options.LogLevel))
            serviceArgs.Add($"--loglevel {options.LogLevel}");

        // 构建完整的二进制路径
        var binaryPath = $"\"{exePath}\" {string.Join(" ", serviceArgs)}";

        try
        {
            ServiceManager.InstallService(
                options.ServiceName,
                binaryPath,
                options.DisplayName ?? "Redis Server",
                options.Description ?? "Redis in-memory data structure store",
                options.StartMode);

            Console.WriteLine($"服务 '{options.ServiceName}' 安装成功。");
            Console.WriteLine();

            // 询问是否启动服务
            if (options.StartMode == "auto")
            {
                Console.WriteLine("正在启动服务...");
                try
                {
                    ServiceManager.StartServiceByName(options.ServiceName);
                    Console.WriteLine("服务已启动。");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"启动服务失败: {ex.Message}");
                    Console.WriteLine($"请手动运行: sc start {options.ServiceName}");
                }
            }
            else
            {
                Console.WriteLine($"使用以下命令启动服务: sc start {options.ServiceName}");
            }

            return 0;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"错误: {ex.Message}");
            return 1;
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            Console.Error.WriteLine($"错误: {ex.Message}");
            Console.Error.WriteLine("请以管理员身份运行此程序。");
            return 1;
        }
    }

    private static int UninstallService(UninstallCommand cmd)
    {
        var serviceName = cmd.Options.ServiceName;

        Console.WriteLine($"正在卸载服务 '{serviceName}'...");

        try
        {
            ServiceManager.UninstallService(serviceName);
            Console.WriteLine($"服务 '{serviceName}' 卸载成功。");
            return 0;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"错误: {ex.Message}");
            return 1;
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            Console.Error.WriteLine($"错误: {ex.Message}");
            Console.Error.WriteLine("请以管理员身份运行此程序。");
            return 1;
        }
    }

    private static async Task<int> RunRedis(RunCommand cmd)
    {
        var options = cmd.Options;

        // 构建配置
        var config = new RedisConfiguration
        {
            ConfigFilePath = options.ConfigFilePath,
            Port = options.Port,
            DataDirectory = options.DataDirectory,
            LogLevel = options.LogLevel
        };

        if (options.Foreground)
        {
            // 前台模式运行
            return await RunForegroundAsync(config);
        }
        else
        {
            // Windows 服务模式运行
            return await RunAsServiceAsync(config);
        }
    }

    private static async Task<int> RunForegroundAsync(RedisConfiguration config)
    {
        Console.WriteLine("正在以前台模式启动 Redis...");

        using var processManager = new RedisProcessManager(config);

        // 处理 Ctrl+C
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("\n正在停止...");
            cts.Cancel();
        };

        // 进程退出时自动关闭
        processManager.ProcessExited += (sender, e) =>
        {
            Console.WriteLine($"Redis 进程已退出 (退出码: {e.ExitCode})");
            cts.Cancel();
        };

        try
        {
            var started = await processManager.StartAsync(cts.Token);
            if (!started)
            {
                Console.Error.WriteLine("启动 Redis 失败");
                return 1;
            }

            Console.WriteLine($"Redis 已启动 (PID: {processManager.ProcessId})");
            Console.WriteLine("按 Ctrl+C 停止...");

            // 等待取消信号
            await Task.Delay(Timeout.Infinite, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // 正常退出
        }
        finally
        {
            Console.WriteLine("正在停止 Redis...");
            await processManager.StopAsync();
        }

        return 0;
    }

    private static async Task<int> RunAsServiceAsync(RedisConfiguration config)
    {
        var host = Host.CreateDefaultBuilder()
            .UseWindowsService()
            .ConfigureLogging(logging =>
            {
#pragma warning disable CA1416 // 平台兼容性警告：AddEventLog 仅在 Windows 上可用
                logging.AddEventLog();
#pragma warning restore CA1416
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton(config);
                services.AddSingleton<RedisProcessManager>();
                services.AddHostedService<RedisBackgroundService>();
            })
            .Build();

        await host.RunAsync();
        return 0;
    }

    #endregion
}

/// <summary>
/// Redis 后台服务（用于 Windows 服务模式）
/// </summary>
public class RedisBackgroundService : BackgroundService
{
    private readonly RedisConfiguration _config;
    private readonly ILogger<RedisBackgroundService> _logger;
    private readonly RedisProcessManager _processManager;

    public RedisBackgroundService(RedisConfiguration config, RedisProcessManager processManager, ILogger<RedisBackgroundService> logger)
    {
        _config = config;
        _processManager = processManager;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("正在启动 Redis 服务...");

        _processManager.ProcessExited += OnProcessExited;

        var started = await _processManager.StartAsync(cancellationToken);
        if (!started)
        {
            _logger.LogError("启动 Redis 进程失败");
            throw new InvalidOperationException("无法启动 Redis 进程");
        }

        _logger.LogInformation("Redis 服务已启动 (PID: {ProcessId})", _processManager.ProcessId);

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 保持服务运行
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("正在停止 Redis 服务...");

        _processManager.ProcessExited -= OnProcessExited;
        await _processManager.StopAsync(cancellationToken);

        _logger.LogInformation("Redis 服务已停止");

        await base.StopAsync(cancellationToken);
    }

    private void OnProcessExited(object? sender, ProcessExitedEventArgs e)
    {
        _logger.LogWarning("Redis 进程意外退出 (退出码: {ExitCode})", e.ExitCode);
    }
}
