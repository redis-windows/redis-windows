using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace RedisService.Service;

/// <summary>
/// Redis 进程管理器
/// </summary>
public class RedisProcessManager : IDisposable
{
    private Process? _redisProcess;
    private readonly RedisConfiguration _config;
    private readonly ILogger<RedisProcessManager>? _logger;
    private readonly SemaphoreSlim _startLock = new(1, 1);
    private bool _disposed;

    /// <summary>
    /// 进程退出事件
    /// </summary>
    public event EventHandler<ProcessExitedEventArgs>? ProcessExited;

    public RedisProcessManager(RedisConfiguration config, ILogger<RedisProcessManager>? logger = null)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// 进程是否正在运行
    /// </summary>
    public bool IsRunning => _redisProcess != null && !_redisProcess.HasExited;

    /// <summary>
    /// 进程 ID
    /// </summary>
    public int? ProcessId => _redisProcess?.Id;

    /// <summary>
    /// 启动 Redis 进程
    /// </summary>
    public async Task<bool> StartAsync(CancellationToken cancellationToken = default)
    {
        await _startLock.WaitAsync(cancellationToken);
        try
        {
            if (_redisProcess != null && !_redisProcess.HasExited)
            {
                _logger?.LogWarning("Redis 进程已在运行中");
                return true;
            }

            var basePath = AppContext.BaseDirectory;
            var redisServerPath = Path.Combine(basePath, "redis-server.exe");

            if (!File.Exists(redisServerPath))
            {
                _logger?.LogError("找不到 redis-server.exe: {Path}", redisServerPath);
                return false;
            }

            var arguments = _config.BuildArguments();
            _logger?.LogInformation("启动 Redis: {Path} {Args}", redisServerPath, arguments);

            var startInfo = new ProcessStartInfo(redisServerPath, arguments)
            {
                WorkingDirectory = basePath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            _redisProcess = Process.Start(startInfo);

            if (_redisProcess == null)
            {
                _logger?.LogError("无法启动 Redis 进程");
                return false;
            }

            // 启用事件处理
            _redisProcess.EnableRaisingEvents = true;
            _redisProcess.Exited += OnProcessExited;
            _redisProcess.OutputDataReceived += OnOutputDataReceived;
            _redisProcess.ErrorDataReceived += OnErrorDataReceived;

            // 开始异步读取输出
            _redisProcess.BeginOutputReadLine();
            _redisProcess.BeginErrorReadLine();

            // 等待一小段时间确认进程已启动
            await Task.Delay(100, cancellationToken);

            if (_redisProcess.HasExited)
            {
                _logger?.LogError("Redis 进程立即退出，退出码: {ExitCode}", _redisProcess.ExitCode);
                return false;
            }

            _logger?.LogInformation("Redis 进程已启动，PID: {ProcessId}", _redisProcess.Id);
            return true;
        }
        finally
        {
            _startLock.Release();
        }
    }

    /// <summary>
    /// 停止 Redis 进程
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_redisProcess == null || _redisProcess.HasExited)
        {
            _logger?.LogDebug("Redis 进程未运行");
            return;
        }

        _logger?.LogInformation("正在停止 Redis 进程...");

        // 尝试优雅关闭
        await TryGracefulShutdownAsync(cancellationToken);

        // 使用优化的异步等待
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_config.GracefulShutdownTimeoutMs);

        try
        {
            await WaitForExitAsync(_redisProcess, cts.Token);
            _logger?.LogInformation("Redis 进程已优雅关闭");
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("优雅关闭超时，强制终止进程");
            try
            {
                _redisProcess.Kill(entireProcessTree: true);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "强制终止进程失败");
            }
        }

        CleanupProcess();
    }

    /// <summary>
    /// 尝试通过 redis-cli 优雅关闭
    /// </summary>
    private async Task TryGracefulShutdownAsync(CancellationToken cancellationToken)
    {
        var basePath = AppContext.BaseDirectory;
        var redisCliPath = Path.Combine(basePath, "redis-cli.exe");

        if (!File.Exists(redisCliPath))
        {
            _logger?.LogWarning("找不到 redis-cli.exe，跳过优雅关闭");
            return;
        }

        // 构建完整的 redis-cli 参数（包含配置文件路径和 dir 参数）
        // 这样 redis-cli 就能找到正确的数据目录进行保存
        var args = _config.BuildCliShutdownArguments();
        _logger?.LogDebug("执行 redis-cli {Args}", args);

        try
        {
            using var cli = Process.Start(new ProcessStartInfo(redisCliPath, args)
            {
                WorkingDirectory = basePath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });

            if (cli != null)
            {
                // 设置超时，避免无限等待
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(10));

                await cli.WaitForExitAsync(cts.Token);
                _logger?.LogDebug("已发送 SHUTDOWN 命令，退出码: {ExitCode}", cli.ExitCode);
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("redis-cli SHUTDOWN 命令超时");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "发送 SHUTDOWN 命令失败");
        }
    }

    /// <summary>
    /// 异步等待进程退出（性能优化：不阻塞线程池）
    /// </summary>
    private static Task WaitForExitAsync(Process process, CancellationToken cancellationToken)
    {
        if (process.HasExited)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource<bool>();

        void OnExited(object? sender, EventArgs e)
        {
            process.Exited -= OnExited;
            tcs.TrySetResult(true);
        }

        process.Exited += OnExited;

        // 处理已退出的情况
        if (process.HasExited)
        {
            process.Exited -= OnExited;
            return Task.CompletedTask;
        }

        // 注册取消
        cancellationToken.Register(() =>
        {
            process.Exited -= OnExited;
            tcs.TrySetCanceled(cancellationToken);
        });

        return tcs.Task;
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        var process = sender as Process;
        var exitCode = process?.ExitCode ?? -1;
        _logger?.LogWarning("Redis 进程意外退出，退出码: {ExitCode}", exitCode);

        ProcessExited?.Invoke(this, new ProcessExitedEventArgs(exitCode, process?.StartTime ?? DateTime.MinValue, DateTime.Now));

        CleanupProcess();
    }

    private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            _logger?.LogInformation("[Redis] {Data}", e.Data);
        }
    }

    private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            _logger?.LogError("[Redis] {Data}", e.Data);
        }
    }

    private void CleanupProcess()
    {
        if (_redisProcess != null)
        {
            try
            {
                _redisProcess.Exited -= OnProcessExited;
                _redisProcess.OutputDataReceived -= OnOutputDataReceived;
                _redisProcess.ErrorDataReceived -= OnErrorDataReceived;
                _redisProcess.Dispose();
            }
            catch { }
            _redisProcess = null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        CleanupProcess();
        _startLock.Dispose();
    }
}

/// <summary>
/// 进程退出事件参数
/// </summary>
public class ProcessExitedEventArgs(int exitCode, DateTime startTime, DateTime exitTime) : EventArgs
{
    public int ExitCode { get; } = exitCode;
    public DateTime StartTime { get; } = startTime;
    public DateTime ExitTime { get; } = exitTime;
    public TimeSpan Duration => ExitTime - StartTime;
}
