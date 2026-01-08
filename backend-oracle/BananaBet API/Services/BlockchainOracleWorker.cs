using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BananaBet_API.Services
{
    /// <summary>
    /// Periodically syncs DB matches with the smart contract.
    /// </summary>
    public class BlockchainOracleWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BlockchainOracleWorker> _logger;

        public BlockchainOracleWorker(
            IServiceProvider serviceProvider,
            ILogger<BlockchainOracleWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Blockchain oracle worker started.");

            try
            {
                _logger.LogInformation("BlockchainOracleWorker: initial delay 20s");
                await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("BlockchainOracleWorker: Starting sync iteration.");
                    using var scope = _serviceProvider.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<BlockchainOracleService>();

                    await service.SyncAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // graceful shutdown
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Oracle sync iteration failed");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(3), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // shutting down
                }
            }
        }
    }
}

