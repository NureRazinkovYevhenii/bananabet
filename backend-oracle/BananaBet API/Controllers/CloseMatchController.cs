using BananaBet_API.DTO;
using BananaBet_API.Models;
using BananaBet_API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BananaBet_API.Controllers
{
    [ApiController]
    [Route("api/oracle/close")]
    public class CloseMatchController : ControllerBase
    {
        private readonly BlockchainClient _blockchain;
        private readonly BettingDbContext _db;

        public CloseMatchController(BlockchainClient blockchain, BettingDbContext db)
        {
            _blockchain = blockchain;
            _db = db;
        }

        [HttpPost("{externalId}/close")]
        public async Task<IActionResult> Close(string externalId, CancellationToken ct)
        {
            if (!ulong.TryParse(externalId, out var parsedId))
                return BadRequest("Invalid externalId");

            var match = await _db.Matches
                .FirstOrDefaultAsync(m => m.ExternalId == externalId, ct);

            if (match == null)
                return NotFound("Match not found");

            var response = await _blockchain.CloseMatchAsync(parsedId, ct);

            switch (response.Result)
            {
                case ChainTxResult.Sent:
                case ChainTxResult.BadStatus:
                    match.Status = response.CurrentStatus.HasValue
                        ? MapChainStatus(response.CurrentStatus.Value)
                        : MatchPipelineStatus.Closed;
                    await SyncBetsAsync(match, parsedId, ct);
                    await _db.SaveChangesAsync(ct);

                    return Ok(new
                    {
                        externalId,
                        tx = response.Tx,
                        status = match.Status.ToString()
                    });

                case ChainTxResult.Failed:
                    return StatusCode(502, "closeMatch failed on-chain");
            }

            return StatusCode(500, "Unexpected closeMatch result");
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

        private async Task SyncBetsAsync(Match match, ulong externalId, CancellationToken ct)
        {
            var onChainBets = await _blockchain.GetBetsByMatchAsync(externalId, ct);
            if (onChainBets.Count == 0)
                return;

            foreach (var ob in onChainBets)
            {
                var existing = await _db.Bets
                    .FirstOrDefaultAsync(b =>
                        b.MatchId == match.Id &&
                        b.OnChainIndex == ob.OnChainIndex, ct);

                if (existing == null)
                {
                    _db.Bets.Add(new Bet
                    {
                        MatchId = match.Id,
                        UserWalletAddress = ob.User.ToLower(),
                        Pick = ob.Side,
                        Amount = ob.Amount,
                        Status = ob.PlayAmount == 0 ? BetStatus.Refunded : BetStatus.Matched,
                        PlayAmount = ob.PlayAmount,
                        OddsAtMoment = ob.Side == 1 ? match.OddsHome : match.OddsAway,
                        BlockchainTxHash = string.Empty,
                        BetTime = DateTime.UtcNow, // No timestamp on chain
                        OnChainIndex = ob.OnChainIndex
                    });
                }
                else
                {
                    existing.PlayAmount = ob.PlayAmount;
                    existing.Status = ob.PlayAmount == 0 ? BetStatus.Refunded : BetStatus.Matched;
                }
            }
        }
    }

}
