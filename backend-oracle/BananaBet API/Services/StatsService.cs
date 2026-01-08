using BananaBet_API.DTO;
using BananaBet_API.Models;
using Microsoft.EntityFrameworkCore;

namespace BananaBet_API.Services
{
    public class StatsService
    {
        private readonly BettingDbContext _db;

        private const int WINDOW = 10;
        private const double AVG_ELO = 1500.0;

        public StatsService(BettingDbContext db)
        {
            _db = db;
        }

        public async Task<MatchFeaturesDto?> BuildFeaturesAsync(
            string homeTeam,
            string awayTeam,
            DateTime matchDate,
            double homeEloSnapshot,
            double awayEloSnapshot
        )
        {
            // =========================
            // LOAD HISTORY
            // =========================
            var history = await _db.HistoricalMatches
                .Where(m =>
                    m.MatchDate < matchDate &&
                    (
                        m.HomeTeam == homeTeam ||
                        m.AwayTeam == homeTeam ||
                        m.HomeTeam == awayTeam ||
                        m.AwayTeam == awayTeam
                    )
                )
                .OrderBy(m => m.MatchDate)
                .ToListAsync();

            if (history.Count < 3)
                return null; // недостаточно данных

            // =========================
            // FORM
            // =========================
            int Form(string team, int n)
            {
                return history
                    .Where(m => m.HomeTeam == team || m.AwayTeam == team)
                    .OrderByDescending(m => m.MatchDate)
                    .Take(n)
                    .Sum(m =>
                    {
                        if (m.HomeTeam == team)
                            return m.HomeTarget > m.AwayTarget ? 3 :
                                   m.HomeTarget == m.AwayTarget ? 1 : 0;
                        else
                            return m.AwayTarget > m.HomeTarget ? 3 :
                                   m.HomeTarget == m.AwayTarget ? 1 : 0;
                    });
            }

            int form3Home = Form(homeTeam, 3);
            int form5Home = Form(homeTeam, 5);
            int form3Away = Form(awayTeam, 3);
            int form5Away = Form(awayTeam, 5);

            // =========================
            // ADJUSTED SHOTS (ROLLING)
            // =========================
            double RollAdj(string team)
            {
                var games = history
                    .Where(m => m.HomeTeam == team || m.AwayTeam == team)
                    .OrderByDescending(m => m.MatchDate)
                    .Take(WINDOW)
                    .Select(m =>
                    {
                        if (m.HomeTeam == team)
                            return m.HomeTarget * (m.AwayElo / AVG_ELO);
                        else
                            return m.AwayTarget * (m.HomeElo / AVG_ELO);
                    })
                    .ToList();

                return games.Count >= 3 ? games.Average() : double.NaN;
            }

            double homeAdj = RollAdj(homeTeam);
            double awayAdj = RollAdj(awayTeam);

            // =========================
            // FINAL FEATURES
            // =========================
            double eloDiffNorm = (homeEloSnapshot - awayEloSnapshot) / 400.0;
            double eloSignedSqrt =
                Math.Sign(eloDiffNorm) * Math.Sqrt(Math.Abs(eloDiffNorm));

            return new MatchFeaturesDto
            {
                Elo_Diff_Norm = eloDiffNorm,
                Elo_Signed_Sqrt = eloSignedSqrt,
                Adj_Shots_Diff = homeAdj - awayAdj,
                Form3_Diff = form3Home - form3Away,
                Form5_Diff = form5Home - form5Away
            };
        }
    }
}
