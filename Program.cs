using System.Diagnostics;

namespace RedisService
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddWindowsService();
            builder.Services.AddHostedService<RedisService>();

            var host = builder.Build();
            host.Run();
        }
    }

    class RedisService : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Process? process = null;

            try
            {
                if (!stoppingToken.IsCancellationRequested)
                {
                    var basePath = Path.Combine(AppContext.BaseDirectory);
                    var diskSymbol = basePath[..basePath.IndexOf(":")];
                    var confPath = basePath.Replace(diskSymbol + ":", "/cygdrive/" + diskSymbol);

                    var processStartInfo = new ProcessStartInfo(Path.Combine(basePath, "redis-server.exe").Replace("\\", "/"), String.Format("\"{0}\"", Path.Combine(confPath, "redis.conf").Replace("\\", "/")))
                    {
                        WorkingDirectory = basePath
                    };

                    process = Process.Start(processStartInfo);
                    if (process != null)
                    {
                        await process.WaitForExitAsync(stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // When the stopping token is canceled, for example, a call made from services.msc,
                // we shouldn't exit with a non-zero exit code. In other words, this is expected...
            }
            finally
            {
                if (process != null)
                {
                    process.Kill();
                    process.Dispose();
                }
            }
        }
    }
}
