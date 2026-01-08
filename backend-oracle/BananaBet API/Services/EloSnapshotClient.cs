using BananaBet_API.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BananaBet_API.Services
{
    /// <summary>
    /// Fetches daily Elo snapshots from ClubElo CSV endpoint.
    /// </summary>
    public class EloSnapshotClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<EloSnapshotClient> _logger;

        public EloSnapshotClient(HttpClient http, ILogger<EloSnapshotClient> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<List<EloSnapshot>> GetDailySnapshotAsync(DateOnly date, CancellationToken ct)
        {
            var url = $"http://api.clubelo.com/{date:yyyy-MM-dd}";

            using var res = await _http.GetAsync(url, ct);
            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync(ct);
                throw new Exception($"ClubElo error: {res.StatusCode} {body}");
            }

            var csv = await res.Content.ReadAsStringAsync(ct);
            return ParseCsv(csv, date);
        }

        private List<EloSnapshot> ParseCsv(string csv, DateOnly date)
        {
            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var snapshots = new List<EloSnapshot>();

            foreach (var line in lines.Skip(1))
            {
                var cols = line.Split(',', StringSplitOptions.None);
                if (cols.Length < 5)
                    continue;

                var clubRaw = cols[1].Trim();
                if (!double.TryParse(cols[4].Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var elo))
                    continue;

                try
                {
                    var club = TeamNameNormalizer.Normalize(clubRaw);
                    snapshots.Add(new EloSnapshot
                    {
                        Date = date,
                        Club = club,
                        Elo = elo
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to normalize club {Club}", clubRaw);
                }
            }

            return snapshots;
        }
    }
}

