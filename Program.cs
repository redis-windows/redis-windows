using System.Diagnostics;

namespace RedisService
{
    class Program
    {
        static void Main(string[] args)
        {
            string configFilePath = "redis.conf";

            if (args.Length > 1 && args[0] == "-c")
            {
                configFilePath = args[1];
            }

            IHost host = Host.CreateDefaultBuilder()
                .UseWindowsService()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService(_ => new RedisService(configFilePath));
                })
                .Build();

            host.Run();
        }
    }

    public class RedisService : BackgroundService
    {
        private readonly string configFilePath;

        private Process? redisProcess;

        private string redisServerPath = string.Empty;

        private string configPathForRedis = string.Empty;


        public RedisService(string configFilePath)
        {
            this.configFilePath = configFilePath;
        }


        public override Task StartAsync(CancellationToken cancellationToken)
        {
            var basePath = AppContext.BaseDirectory;
            string conf = configFilePath;

            if (!Path.IsPathRooted(conf))
                conf = Path.Combine(basePath, conf);

            conf = Path.GetFullPath(conf);

            var diskSymbol = conf[..conf.IndexOf(":")];
            configPathForRedis = conf
                .Replace(diskSymbol + ":", "/cygdrive/" + diskSymbol)
                .Replace("\\", "/");

            redisServerPath = Path.Combine(basePath, "redis-server.exe")
                .Replace("\\", "/");

            string arguments = $"\"{configPathForRedis}\"";

            redisProcess = Process.Start(new ProcessStartInfo(redisServerPath, arguments)
            {
                WorkingDirectory = basePath,
                UseShellExecute = false
            });

            return Task.CompletedTask;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }


        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (redisProcess == null || redisProcess.HasExited)
                return;

            try
            {
                await TryGracefulShutdownAsync();

                bool exited = await WaitForExitAsync(redisProcess, 5000);

                if (!exited)
                {
                    redisProcess.Kill(true);
                }
            }
            catch
            {
                if (!redisProcess.HasExited)
                    redisProcess.Kill(true);
            }

            redisProcess.Dispose();
        }


        private async Task TryGracefulShutdownAsync()
        {
            string redisCliPath = Path.Combine(AppContext.BaseDirectory, "redis-cli.exe");

            if (!File.Exists(redisCliPath))
                return;

            var psi = new ProcessStartInfo(redisCliPath, "SHUTDOWN")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            try
            {
                using var cli = Process.Start(psi);
                if (cli != null)
                    await cli.WaitForExitAsync();
            }
            catch
            {
            }
        }


        private static Task<bool> WaitForExitAsync(Process process, int timeoutMs)
        {
            return Task.Run(() => process.WaitForExit(timeoutMs));
        }
    }
}
