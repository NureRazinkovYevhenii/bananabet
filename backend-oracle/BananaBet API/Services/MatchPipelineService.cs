using BananaBet_API.Models;
using BananaBet_API.DTO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BananaBet_API.Services
{
    public class MatchPipelineService
    {
        private readonly FootballDataClient _football;
        private readonly StatsService _stats;
        private readonly MlClient _ml;
        private readonly BettingDbContext _db;
        private readonly ILogger<MatchPipelineService> _logger;
        private readonly string _competitionCode;

        public MatchPipelineService(
            FootballDataClient football,
            StatsService stats,
            MlClient ml,
            BettingDbContext db,
            IConfiguration config,
            ILogger<MatchPipelineService> logger)
        {
            _football = football;
            _stats = stats;
            _ml = ml;
            _db = db;
            _logger = logger;
            _competitionCode = config["FootballData:CompetitionCode"] ?? "PL";
        }

        public async Task FetchTomorrowMatchesAsync(CancellationToken cancellationToken)
        {
            var targetDate = DateTime.UtcNow.Date.AddDays(1);

            bool alreadyLoaded = await _db.Matches
                .AnyAsync(m => m.StartTime.Date == targetDate, cancellationToken);

            if (alreadyLoaded)
            {
                _logger.LogInformation("Matches for {Date} already exist. Skipping fetch.", targetDate);
                return;
            }

            var upcoming = await _football.GetUpcomingMatchesAsync(_competitionCode, 50);
            var tomorrowMatches = upcoming
                .Where(m => m.StartTime.Date == targetDate)
                .ToList();

            if (!tomorrowMatches.Any())
            {
                _logger.LogInformation("No matches to fetch for {Date}", targetDate);
                return;
            }

            foreach (var u in tomorrowMatches)
            {
                bool exists = await _db.Matches.AnyAsync(
                    m => m.ExternalId == u.ExternalId,
                    cancellationToken);

                if (exists)
                {
                    _logger.LogInformation("Match {ExternalId} already exists. Skipping.", u.ExternalId);
                    continue;
                }

                var match = new Match
                {
                    ExternalId = u.ExternalId,
                    HomeTeam = TeamNameNormalizer.Normalize(u.HomeTeam),
                    AwayTeam = TeamNameNormalizer.Normalize(u.AwayTeam),
                    StartTime = DateTime.SpecifyKind(u.StartTime, DateTimeKind.Utc),
                    OddsHome = 0,
                    OddsAway = 0,
                    Status = MatchPipelineStatus.Fetched
                };

                _db.Matches.Add(match);
            }

            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Saved {Count} matches for {Date}", tomorrowMatches.Count, targetDate);
        }

        public async Task CalculateOddsAsync(CancellationToken cancellationToken)
        {
            var fetched = await _db.Matches
                .Where(m => m.Status == MatchPipelineStatus.Fetched)
                .ToListAsync(cancellationToken);

            if (!fetched.Any())
            {
                _logger.LogDebug("No matches with status Fetched to process.");
                return;
            }

            foreach (var match in fetched)
            {
                var matchDate = DateOnly.FromDateTime(match.StartTime);

                var homeElo = await _db.EloSnapshots
                    .Where(e =>
                        e.Club == match.HomeTeam &&
                        e.Date <= matchDate
                    )
                    .OrderByDescending(e => e.Date)
                    .Select(e => (double?)e.Elo)
                    .FirstOrDefaultAsync(cancellationToken);

                var awayElo = await _db.EloSnapshots
                    .Where(e =>
                        e.Club == match.AwayTeam &&
                        e.Date <= matchDate
                    )
                    .OrderByDescending(e => e.Date)
                    .Select(e => (double?)e.Elo)
                    .FirstOrDefaultAsync(cancellationToken);

                if (homeElo == null || awayElo == null)
                {
                    _logger.LogWarning(
                        "Elo snapshot missing for {Date}: {Home} ({HomeHas}) / {Away} ({AwayHas})",
                        matchDate,
                        match.HomeTeam,
                        homeElo != null,
                        match.AwayTeam,
                        awayElo != null);
                    continue;
                }

                var features = await _stats.BuildFeaturesAsync(
                    match.HomeTeam,
                    match.AwayTeam,
                    match.StartTime,
                    homeElo.Value,
                    awayElo.Value);

                if (features == null)
                {
                    _logger.LogWarning(
                        "Insufficient history for match {ExternalId} ({Home} vs {Away})",
                        match.ExternalId,
                        match.HomeTeam,
                        match.AwayTeam);
                    continue;
                }

                var prediction = await _ml.PredictAsync(features);

                match.OddsHome = (decimal)prediction.Fair_Odd_Home;
                match.OddsAway = (decimal)prediction.Fair_Odd_Away;

                match.Status = MatchPipelineStatus.OddsCalculated;
                match.Status = MatchPipelineStatus.ReadyForChain;

                _logger.LogInformation(
                    "Calculated odds for {ExternalId}: {Home} vs {Away}",
                    match.ExternalId,
                    match.HomeTeam,
                    match.AwayTeam);
            }

            await _db.SaveChangesAsync(cancellationToken);
        }
    }

}
