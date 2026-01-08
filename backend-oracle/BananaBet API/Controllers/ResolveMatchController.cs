using BananaBet_API.DTO;
using BananaBet_API.Models;
using BananaBet_API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BananaBet_API.Controllers
{


    [ApiController]
    [Route("api/oracle/resolve")]
    public class ResolveMatchController : ControllerBase
    {
        private readonly BlockchainClient _blockchain;
        private readonly BettingDbContext _db;

        public ResolveMatchController(BlockchainClient blockchain, BettingDbContext db)
        {
            _blockchain = blockchain;
            _db = db;
        }

        [HttpPost("{externalId}/resolve")]
        public async Task<IActionResult> Resolve(
            string externalId,
            [FromQuery] byte result, // 1 = Home, 2 = Away
            CancellationToken ct)
        {
            if (result != 1 && result != 2)
                return BadRequest("Result must be 1 (Home) or 2 (Away)");

            if (!ulong.TryParse(externalId, out var parsedId))
                return BadRequest("Invalid externalId");

            var match = await _db.Matches
                .Include(m => m.Bets)
                .FirstOrDefaultAsync(m => m.ExternalId == externalId, ct);

            if (match == null)
                return NotFound("Match not found");

            var response =
                await _blockchain.ResolveMatchAsync(parsedId, result, ct);

            switch (response.Result)
            {
                case ChainTxResult.Sent:
                case ChainTxResult.BadStatus:
                    match.Status = response.CurrentStatus.HasValue
                        ? MapChainStatus(response.CurrentStatus.Value)
                        : MatchPipelineStatus.Resolved;
                    match.Result = result == 1 ? "Home" : "Away";

                    UpdateBetsStatus(match, result);
                    await _db.SaveChangesAsync(ct);

                    return Ok(new
                    {
                        externalId,
                        result = match.Result,
                        tx = response.Tx
                    });

                case ChainTxResult.Failed:
                    // ❗ не міняємо стан
                    return StatusCode(502, "resolveMatch failed on-chain");
            }

            return StatusCode(500, "Unexpected resolveMatch result");
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
