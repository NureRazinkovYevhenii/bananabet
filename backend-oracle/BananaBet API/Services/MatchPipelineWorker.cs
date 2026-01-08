using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BananaBet_API.Services
{
    public class MatchPipelineWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MatchPipelineWorker> _logger;

        public MatchPipelineWorker(
            IServiceProvider serviceProvider,
            ILogger<MatchPipelineWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Match pipeline worker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var pipeline = scope.ServiceProvider.GetRequiredService<MatchPipelineService>();

                    var utcNow = DateTime.UtcNow;

                    if (true)
                    {
                        await pipeline.FetchTomorrowMatchesAsync(stoppingToken);
                    }

                    await pipeline.CalculateOddsAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Match pipeline iteration failed");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }
    }
}

