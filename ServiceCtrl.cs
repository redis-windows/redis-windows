using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using System.Timers;
using StackExchange.Redis;

namespace RedisServiceCtrl
{
    public partial class ServiceCtrl : ServiceBase
    {
        // 配置参数
        private readonly string RedisExeName = "redis-server.exe";
        private readonly string ConfigFileName = "myredis.conf";
        private readonly string LogDirectoryName = "Logs";
        private readonly int HealthCheckInterval = 30000;
        private readonly int MaxRestartAttempts = 3;
        
        // 运行时状态
        private Process _redisProcess;
        private ConnectionMultiplexer _redisConnection;
        private Timer _healthCheckTimer;
        private int _restartCount;
        private RedisConfig _config;
        public ServiceCtrl()
        {
            InitializeComponent();
        }
        //测试启动
        public void myStart()
        {
            OnStart(null);
        }
        //测试停止
        public void myStop()
        {
            OnStop();
        }
        //服务启动
        protected override void OnStart(string[] args)
        {
            try
            {
                Log("服务启动初始化...", EventLogEntryType.Information);
                // 初始化工作目录
                var appPath = GetApplicationBasePath();
                Directory.SetCurrentDirectory(appPath);
                // 清理残留进程
                CleanupLegacyProcesses();
                // 加载配置
                LoadRedisConfig();
                // 启动Redis
                StartRedisServer();
                // 初始化健康检查
                InitializeHealthCheck();
                Log("服务启动操作完成", EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                Log($"启动失败: {ex.Message}", EventLogEntryType.Error);
                Stop();
            }
        }
        //服务停止
        protected override void OnStop()
        {
            try
            {
                Log("正在停止服务...", EventLogEntryType.Information);
                // 停止健康检查
                if(null != _healthCheckTimer)
                {
                    _healthCheckTimer?.Stop();
                    _healthCheckTimer?.Dispose();
                }
                // 关闭Redis连接
                StopRedisServer();
                // 清理残留进程
                CleanupLegacyProcesses();
                Log("服务已停止", EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                Log($"停止服务时发生错误: {ex.Message}", EventLogEntryType.Error);
            }
        }
        private void CleanupLegacyProcesses()
        {
            var list = Process.GetProcessesByName("redis-server");
            foreach (Process proc in list)
            {
                try
                {
                    Log($"终止残留进程 PID:{proc.Id}", EventLogEntryType.Warning);
                    if(!proc.HasExited)
                    {
                        proc.Kill();
                        proc.WaitForExit(5000);
                    }
                }
                catch (Exception ex)
                {
                    Log($"进程终止失败: {ex.Message}", EventLogEntryType.Error);
                }
            }
            // 移除 myredis.pid 文件（redis服务存在可能残留.pid文件的情形）
            string pidFilePath = Path.Combine(GetApplicationBasePath(), "myredis.pid");
            if (File.Exists(pidFilePath))
            {
                try
                {
                    File.Delete(pidFilePath);
                    Log($"成功删除 PID 文件: {pidFilePath}", EventLogEntryType.Information);
                }
                catch (Exception ex)
                {
                    Log($"删除 PID 文件失败: {ex.Message}", EventLogEntryType.Error);
                }
            }
        }
        private void LoadRedisConfig()
        {
            var configPath = Path.Combine(GetApplicationBasePath(), ConfigFileName);
            if (!File.Exists(configPath))
                throw new FileNotFoundException("Redis配置文件不存在");
            _config = new RedisConfig();
            foreach (var line in File.ReadLines(configPath))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("#") || string.IsNullOrEmpty(trimmed)) 
                    continue;
                var match = Regex.Match(trimmed, @"^(bind|port|requirepass)\s+(.+)$");
                if (match.Success)
                {
                    switch (match.Groups[1].Value.ToLower())
                    {
                        case "bind":
                            _config.BindAddresses.AddRange(match.Groups[2].Value.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries));
                            break;
                        case "port":
                            if (int.TryParse(match.Groups[2].Value, out int port))
                                _config.Port = port;
                            break;
                        case "requirepass":
                            _config.Password = match.Groups[2].Value;
                            break;
                    }
                }
            }
            if (_config.BindAddresses.Count == 0) 
                _config.BindAddresses.Add("127.0.0.1");
            if (_config.Port == 0) 
                _config.Port = 6379;
        }
        private void StartRedisServer()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(GetApplicationBasePath(), RedisExeName),
                Arguments = ConfigFileName,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                WorkingDirectory = GetApplicationBasePath()
            };
            _redisProcess = new Process { StartInfo = startInfo };
            _redisProcess.OutputDataReceived += (s, e) => Log($"Redis输出: {e.Data}", EventLogEntryType.Information);
            _redisProcess.Start();
            _redisProcess.BeginOutputReadLine();
        }
        private void InitializeHealthCheck()
        {
            _healthCheckTimer = new Timer(HealthCheckInterval);
            _healthCheckTimer.Elapsed += (s, e) => PerformHealthCheck();
            _healthCheckTimer.Start();
        }
        private void PerformHealthCheck()
        {
            try
            {
                if (null != _redisProcess && _redisProcess.HasExited)
                {
                    throw new Exception("redis服务进程未正常运行");
                }
                if (null == _redisConnection || !_redisConnection.IsConnected)
                {
                    _redisConnection = TryConnectToRedis();
                }
                if (null != _redisConnection && _redisConnection.IsConnected)
                {
                    var db = _redisConnection.GetDatabase();
                    if (db.Ping().TotalMilliseconds > 1000)
                        throw new Exception("心跳响应延迟过高");

                    _restartCount = 0;
                }
                else
                {
                    throw new Exception("无法连接到Redis");
                }
            }
            catch (Exception ex)
            {
                HandleHealthCheckFailure(ex);
            }
        }
        private ConnectionMultiplexer TryConnectToRedis()
        {
            var bindAddresses = _config.BindAddresses;
            if (bindAddresses.Contains("127.0.0.1"))
            {
                bindAddresses.Remove("127.0.0.1");
                bindAddresses.Insert(0, "127.0.0.1");
            }
            //连接，并优先127
            foreach (var ip in bindAddresses)
            {
                try
                {
                    var options = new ConfigurationOptions
                    {
                        EndPoints = { $"{ip}:{_config.Port}" },
                        Password = _config.Password,
                        ConnectTimeout = 5000,
                        AllowAdmin = true
                    };
                    var connection = ConnectionMultiplexer.Connect(options);
                    Log($"成功连接到Redis: {ip}:{_config.Port}", EventLogEntryType.Information);
                    return connection;
                }
                catch (Exception ex)
                {
                    Log($"连接到Redis失败 ({ip}:{_config.Port}): {ex.Message}", EventLogEntryType.Warning);
                }
            }
            return null;
        }
        private void HandleHealthCheckFailure(Exception ex)
        {
            Log($"连续 {++_restartCount}/{MaxRestartAttempts}次健康检查失败: {ex.Message}", EventLogEntryType.Warning);
            if (_restartCount >= MaxRestartAttempts)
            {
                Log($"达到最大重启次数，停止服务", EventLogEntryType.Error);
                Stop();
                return;
            }
            try
            {
                StopRedisServer();
                StartRedisServer();
                Log($"第 {_restartCount} 次重启", EventLogEntryType.Warning);
            }
            catch (Exception restartEx)
            {
                Log($"重启失败: {restartEx.Message}", EventLogEntryType.Error);
            }
        }
        private void StopRedisServer()
        {
            try
            {
                if (null != _redisProcess && !_redisProcess.HasExited)
                {
                    if (null == _redisConnection || !_redisConnection.IsConnected)
                    {
                        _redisConnection = TryConnectToRedis();
                    }
                    if (_redisConnection != null && _redisConnection.IsConnected)
                    {
                        var endpoints = _redisConnection.GetEndPoints();
                        foreach (var endpoint in endpoints)
                        {
                            try
                            {
                                _redisConnection.GetServer(endpoint).Shutdown();
                                Log($"关闭Redis连接: {endpoint}", EventLogEntryType.Information);
                            }
                            catch (Exception ex)
                            {
                                Log($"关闭Redis连接异常 ({endpoint}): {ex.Message}", EventLogEntryType.Warning);
                            }
                        }
                        _redisConnection.Dispose();
                    }
                    System.Threading.Thread.Sleep(3000);//延时一下等shutdown执行完
                }
            }
            catch (Exception ex)
            {
                Log($"关闭连接异常: {ex.Message}", EventLogEntryType.Warning);
            }
            try
            {
                if (null != _redisProcess && !_redisProcess.HasExited)
                {
                    if (!_redisProcess.WaitForExit(5000))
                    {
                        _redisProcess.Kill();
                        Log("强制终止Redis进程", EventLogEntryType.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"进程终止异常: {ex.Message}", EventLogEntryType.Error);
            }
            finally
            {
                if (_redisProcess != null)
                    _redisProcess.Dispose();
            }
        }
        private string GetApplicationBasePath()
        {
            return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }
        private void Log(string message, EventLogEntryType type)
        {
            try
            {
                var logDir = Path.Combine(GetApplicationBasePath(), LogDirectoryName);
                Directory.CreateDirectory(logDir);
                var logFile = Path.Combine(logDir, $"{DateTime.Now:yyyyMMdd}.log");
                File.AppendAllText(logFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{type}] {message}\n");
                // 清理旧日志
                foreach (var file in Directory.GetFiles(logDir, "*.log"))
                {
                    if ((DateTime.Now - File.GetCreationTime(file)).TotalDays > 30)
                        File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry($"日志记录失败: {ex.Message}", EventLogEntryType.Error);
            }
        }
        private class RedisConfig
        {
            public List<string> BindAddresses { get; set; } = new List<string>();
            public int Port { get; set; }
            public string Password { get; set; }
        }
    }
}



