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

            IHost host = Host.CreateDefaultBuilder().UseWindowsService().ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService(serviceProvider =>
                        new RedisService(configFilePath));
            }).Build();

            host.Run();
        }
    }



    public class RedisService(string configFilePath) : BackgroundService
    {

        private Process? redisProcess = new();


        public override Task StartAsync(CancellationToken stoppingToken)
        {

            var basePath = Path.Combine(AppContext.BaseDirectory);

            if (!Path.IsPathRooted(configFilePath))
            {
                configFilePath = Path.Combine(basePath, configFilePath);
            }

            configFilePath = Path.GetFullPath(configFilePath);

            var diskSymbol = configFilePath[..configFilePath.IndexOf(":")];
            var fileConf = configFilePath.Replace(diskSymbol + ":", "/cygdrive/" + diskSymbol).Replace("\\", "/");

            string fileName = Path.Combine(basePath, "redis-server.exe").Replace("\\", "/");
            string arguments = $"\"{fileConf}\"";

            ProcessStartInfo processStartInfo = new(fileName, arguments)
            {
                WorkingDirectory = basePath
            };

            redisProcess = Process.Start(processStartInfo);

            return Task.CompletedTask;
        }



        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(-1, stoppingToken);
        }



        public override Task StopAsync(CancellationToken stoppingToken)
        {
            if (redisProcess != null)
            {
                redisProcess.Kill();
                redisProcess.Dispose();
            }

            return Task.CompletedTask;
        }
    }

}
