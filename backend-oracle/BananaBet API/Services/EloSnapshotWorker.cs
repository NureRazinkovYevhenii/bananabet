using BananaBet_API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BananaBet_API.Services
{
    /// <summary>
    /// Ensures daily Elo snapshot is fetched once per day.
    /// </summary>
    public class EloSnapshotWorker : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<EloSnapshotWorker> _logger;

        public EloSnapshotWorker(IServiceProvider services, ILogger<EloSnapshotWorker> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Elo snapshot worker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<BettingDbContext>();
                    var client = scope.ServiceProvider.GetRequiredService<EloSnapshotClient>();

                    var today = DateOnly.FromDateTime(DateTime.UtcNow);
                    bool exists = await db.EloSnapshots.AnyAsync(e => e.Date == today, stoppingToken);

                    if (!exists)
                    {
                        var snapshots = await client.GetDailySnapshotAsync(today, stoppingToken);
                        if (snapshots.Count > 0)
                        {
                            db.EloSnapshots.AddRange(snapshots);
                            await db.SaveChangesAsync(stoppingToken);
                            _logger.LogInformation("Saved {Count} Elo snapshots for {Date}", snapshots.Count, today);
                        }
                        else
                        {
                            _logger.LogWarning("No Elo snapshots fetched for {Date}", today);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // shutting down
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Elo snapshot worker iteration failed");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // stopping
                }
            }
        }
    }
}

