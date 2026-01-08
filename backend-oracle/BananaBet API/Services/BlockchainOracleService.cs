using BananaBet_API.DTO;
using BananaBet_API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Numerics;

namespace BananaBet_API.Services
{
    /// <summary>
    /// Orchestrates match lifecycle on-chain. All state transitions are idempotent.
    /// </summary>
    public class BlockchainOracleService
    {
        private readonly BettingDbContext _db;
        private readonly BlockchainClient _blockchain;
        private readonly FootballDataClient _football;
        private readonly ILogger<BlockchainOracleService> _logger;

        public BlockchainOracleService(
            BettingDbContext db,
            BlockchainClient blockchain,
            FootballDataClient football,
            ILogger<BlockchainOracleService> logger)
        {
            _db = db;
            _blockchain = blockchain;
            _football = football;
            _logger = logger;
        }

        public async Task SyncAsync(CancellationToken ct)
        {
            await CreateOnChainAsync(ct);
            await OpenMatchesAsync(ct);
            await CloseMatchesAsync(ct);
            await ResolveMatchesAsync(ct);
        }

        private async Task CreateOnChainAsync(CancellationToken ct)
        {
            var ready = await _db.Matches
                .Where(m => m.Status == MatchPipelineStatus.ReadyForChain)
                .ToListAsync(ct);

            if (!ready.Any())
                return;

            foreach (var match in ready)
            {
                if (!TryParseExternalId(match.ExternalId, out var externalId))
                    continue;

                var response = await _blockchain.CreateMatchAsync(
                    externalId,
                    match.OddsHome,
                    match.OddsAway,
                    ct
                );

                switch (response.Result)
                {
                    case ChainTxResult.Sent:
                    case ChainTxResult.AlreadyExists:
                        match.Status = MapChainStatus(response.CurrentStatus ?? OnChainMatchStatus.Created);
                        break;

                    case ChainTxResult.Failed:
                        match.Status = MatchPipelineStatus.ReadyForChain;
                        break;
                }
            }

            await _db.SaveChangesAsync(ct);
        }

        private async Task OpenMatchesAsync(CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var onChain = await _db.Matches
                .Where(m => m.Status == MatchPipelineStatus.OnChain &&
                            m.StartTime - now > TimeSpan.FromMinutes(10))
                .ToListAsync(ct);

            if (!onChain.Any())
                return;

            foreach (var match in onChain)
            {
                if (!TryParseExternalId(match.ExternalId, out var externalId))
                    continue;

                var response = await _blockchain.OpenMatchAsync(externalId, ct);

                switch (response.Result)
                {
                    case ChainTxResult.Sent:
                        match.Status = MapChainStatus(response.CurrentStatus ?? OnChainMatchStatus.Open);
                        break;

                    case ChainTxResult.BadStatus:
                        if (response.CurrentStatus.HasValue)
                            match.Status = MapChainStatus(response.CurrentStatus.Value);
                        else
                            match.Status = MatchPipelineStatus.Open;
                        break;

                    case ChainTxResult.Failed:
                        match.Status = MatchPipelineStatus.OnChain;
                        break;
                }
            }

            await _db.SaveChangesAsync(ct);
        }

        private async Task CloseMatchesAsync(CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var open = await _db.Matches
                .Where(m => m.Status == MatchPipelineStatus.Open &&
                            now >= m.StartTime)
                .ToListAsync(ct);

            if (!open.Any())
                return;

            foreach (var match in open)
            {
                if (!TryParseExternalId(match.ExternalId, out var externalId))
                    continue;

                var response = await _blockchain.CloseMatchAsync(externalId, ct);

                switch (response.Result)
                {
                    case ChainTxResult.Sent:
                    case ChainTxResult.BadStatus:
                        if (response.CurrentStatus.HasValue)
                            match.Status = MapChainStatus(response.CurrentStatus.Value);
                        else
                            match.Status = MatchPipelineStatus.Closed;
                        await SyncBetsAsync(match, externalId, ct);
                        break;

                    case ChainTxResult.Failed:
                        match.Status = MatchPipelineStatus.Open;
                        break;
                }
            }

            await _db.SaveChangesAsync(ct);
        }

        private async Task ResolveMatchesAsync(CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var closed = await _db.Matches
                .Include(m => m.Bets)
                .Where(m => m.Status == MatchPipelineStatus.Closed &&
                            now >= m.StartTime.AddMinutes(150)) // 2.5h
                .ToListAsync(ct);

            if (!closed.Any())
                return;

            foreach (var match in closed)
            {
                if (!TryParseExternalId(match.ExternalId, out var externalId))
                    continue;

                var matchResult = await _football.GetMatchResultAsync(match.ExternalId);
                if (matchResult == null || !matchResult.Finished)
                    continue;

                byte result = matchResult.HomeGoals > matchResult.AwayGoals ? (byte)1 :
                              matchResult.AwayGoals > matchResult.HomeGoals ? (byte)2 : (byte)0;

                var response =
                    await _blockchain.ResolveMatchAsync(externalId, result, ct);

                switch (response.Result)
                {
                    case ChainTxResult.Sent:
                    case ChainTxResult.BadStatus:
                        match.Status = response.CurrentStatus.HasValue
                            ? MapChainStatus(response.CurrentStatus.Value)
                            : MatchPipelineStatus.Resolved;
                        match.Result = result switch
                        {
                            0 => "Draw",
                            1 => "Home",
                            2 => "Away",
                            _ => "Unknown"
                        };
                        UpdateBetsStatus(match, result);
                        break;

                    case ChainTxResult.Failed:
                        match.Status = MatchPipelineStatus.Closed;
                        break;
                }
            }

            await _db.SaveChangesAsync(ct);
        }

        private bool TryParseExternalId(string externalIdRaw, out BigInteger externalId)
        {
            if (BigInteger.TryParse(externalIdRaw, out externalId))
                return true;

            _logger.LogWarning("ExternalId {ExternalId} is not numeric, skipping on-chain sync", externalIdRaw);
            return false;
        }

        private static MatchPipelineStatus MapChainStatus(OnChainMatchStatus status) =>
            status switch
            {
                OnChainMatchStatus.Created => MatchPipelineStatus.OnChain,
                OnChainMatchStatus.Open => MatchPipelineStatus.Open,
                OnChainMatchStatus.Closed => MatchPipelineStatus.Closed,
                OnChainMatchStatus.Resolved => MatchPipelineStatus.Resolved,
                _ => MatchPipelineStatus.OnChain
            };

        private async Task SyncBetsAsync(Match match, BigInteger externalId, CancellationToken ct)
        {
            var onChainBets = await _blockchain.GetBetsByMatchAsync(externalId, ct);
            if (onChainBets.Count == 0)
                return;

            // idempotent update by OnChainIndex
            foreach (var ob in onChainBets)
            {
                var existing = await _db.Bets
                    .FirstOrDefaultAsync(b =>
                        b.MatchId == match.Id &&
                        b.OnChainIndex == ob.OnChainIndex, ct);

                if (existing == null)
                {
                    // create minimal record if not present
                    _db.Bets.Add(new Bet
                    {
                        MatchId = match.Id,
                        UserWalletAddress = ob.User,
                        Pick = ob.Side,
                        Status = ob.PlayAmount == 0 ? BetStatus.Refunded : BetStatus.Matched,
                        Amount = ob.Amount,
                        PlayAmount = ob.PlayAmount,
                        OddsAtMoment = ob.Side == 1 ? match.OddsHome : match.OddsAway,
                        BlockchainTxHash = string.Empty,
                        OnChainIndex = ob.OnChainIndex,
                        BetTime = DateTime.UtcNow
                    });
                }
                else
                {
                    existing.PlayAmount = ob.PlayAmount;
                    existing.Status = ob.PlayAmount == 0 ? BetStatus.Refunded : BetStatus.Matched;
                }
            }
        }

        private void UpdateBetsStatus(Match match, byte result)
        {
            if (match.Bets == null) return;

            foreach (var bet in match.Bets)
            {
                // Only process bets that were successfully matched
                if (bet.Status != BetStatus.Matched)
                    continue;

                if (bet.Pick == result)
                {
                    bet.Status = BetStatus.Win;
                }
                else
                {
                    bet.Status = BetStatus.Lose;
                }
            }
        }
    }
}

